using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemErasmusRazmjena.Data;
using SistemErasmusRazmjena.Models;
using SistemErasmusRazmjena.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SistemErasmusRazmjena.Controllers
{
    [Authorize]
    public class SearchController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public SearchController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View(new SearchViewModel());
        }

        [HttpGet]
        public async Task<IActionResult> Results(string? query, int? semester = null, string? status = null)
        {
            var currentUser = await _userManager.GetUserAsync(User); // Retrieve the current user

            var viewModel = new SearchViewModel
            {
                Query = query ?? "",
                SelectedSemester = semester,
                SelectedStatus = status
            };

            if (string.IsNullOrWhiteSpace(query) && semester == null && string.IsNullOrWhiteSpace(status))
            {
                return View("Index", viewModel);
            }

            // Normalize search query
            var normalizedQuery = NormalizeText(query ?? "");

            // First fetch the data, then filter in memory
            // Search ErasmusPrograms
            var allPrograms = await _context.ErasmusProgrami.ToListAsync();

            // Apply semester filter if provided
            if (semester.HasValue)
            {
                allPrograms = allPrograms.Where(p => p.Semestar == semester.Value).ToList();
            }

            // Apply text search if provided
            if (!string.IsNullOrWhiteSpace(query))
            {
                allPrograms = allPrograms
                    .Where(p =>
                        NormalizeText(p.Univerzitet).Contains(normalizedQuery) ||
                        NormalizeText(p.AkademskaGodina).Contains(normalizedQuery) ||
                        NormalizeText(p.Opis ?? "").Contains(normalizedQuery) ||
                        NormalizeText(p.Semestar.ToString()).Contains(normalizedQuery) ||
                        // Match "Winter" for semester 1
                        (NormalizeText("zimski").Contains(normalizedQuery) && p.Semestar == 1) ||
                        // Match "Summer" for semester 2
                        (NormalizeText("ljetni").Contains(normalizedQuery) && p.Semestar == 2))
                    .ToList();
            }

            viewModel.Programs = allPrograms;

            // Search Prijave - fetch based on role first
            var prijaveQuery = _context.Prijave
                .Include(p => p.Student)
                .Include(p => p.ErasmusProgram)
                .Include(p => p.Dokumentacija)
                .Include(p => p.PrijedlogPredmeta).ThenInclude(pp => pp.Rows)
                .AsQueryable();

            // Filter based on role
            if (User.IsInRole("Student") && currentUser != null)
            {
                // Students can only see their own applications
                prijaveQuery = prijaveQuery.Where(p => p.StudentID == currentUser.Id);
            }
            else if (User.IsInRole("ECTSKoordinator") && currentUser != null)
            {
                // ECTS Coordinators can see applications from their faculty
                prijaveQuery = prijaveQuery.Where(p => p.Student != null && p.Student.FakultetID == currentUser.FakultetID);
            }
            // Admins can see all applications

            // First get all applications
            var prijave = await prijaveQuery.ToListAsync();

            // Apply status filter if provided
            if (!string.IsNullOrWhiteSpace(status))
            {
                StatusPrijave? statusEnum = null;

                if (Enum.TryParse<StatusPrijave>(status, true, out var parsedStatus))
                {
                    statusEnum = parsedStatus;
                }

                if (statusEnum.HasValue)
                {
                    prijave = prijave.Where(p => p.Status == statusEnum.Value).ToList();
                }
            }

            // Apply text search if provided
            if (!string.IsNullOrWhiteSpace(query))
            {
                prijave = prijave
                    .Where(p =>
                        NormalizeText(p.ErasmusProgram?.Univerzitet ?? "").Contains(normalizedQuery) ||
                        NormalizeText(p.ErasmusProgram?.AkademskaGodina ?? "").Contains(normalizedQuery) ||
                        NormalizeText(p.Status.ToString()).Contains(normalizedQuery) ||
                        NormalizeText(p.Student?.UserName ?? "").Contains(normalizedQuery) ||
                        NormalizeText((p.Student?.FirstName ?? "") + " " + (p.Student?.LastName ?? "")).Contains(normalizedQuery) ||
                        // Match by semester name
                        (NormalizeText("zimski").Contains(normalizedQuery) && p.ErasmusProgram?.Semestar == 1) ||
                        (NormalizeText("ljetni").Contains(normalizedQuery) && p.ErasmusProgram?.Semestar == 2) ||
                        // Match by status text variations
                        (NormalizeText("in progress").Contains(normalizedQuery) && p.Status == StatusPrijave.UTOKU) ||
                        (NormalizeText("approved").Contains(normalizedQuery) && p.Status == StatusPrijave.USPJESNA) ||
                        (NormalizeText("rejected").Contains(normalizedQuery) && p.Status == StatusPrijave.NEUSPJESNA) ||
                        p.PrijedlogPredmeta?.Rows.Any(r =>
                            NormalizeText(r.PredmetHome ?? "").Contains(normalizedQuery) ||
                            NormalizeText(r.PredmetAccepting ?? "").Contains(normalizedQuery)) == true)
                    .ToList();
            }

            // Apply semester filter for applications if provided
            if (semester.HasValue)
            {
                prijave = prijave.Where(p => p.ErasmusProgram?.Semestar == semester.Value).ToList();
            }

            // Organize applications by status
            var allPrijave = prijave;
            viewModel.Applications = new PrijavaSegmentedViewModel
            {
                UTOKU = allPrijave.Where(p => p.Status == StatusPrijave.UTOKU).ToList(),
                USPJESNA = allPrijave.Where(p => p.Status == StatusPrijave.USPJESNA).ToList(),
                NEUSPJESNA = allPrijave.Where(p => p.Status == StatusPrijave.NEUSPJESNA).ToList()
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Suggestions(string query)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                return Json(new List<string>());

            // Normalize search query
            var normalizedQuery = NormalizeText(query);

            // Get suggestions from different sources
            var suggestions = new HashSet<string>();

            // Fetch all universities first, then filter in memory
            var allUniversities = await _context.ErasmusProgrami
                .Select(p => p.Univerzitet)
                .Distinct()
                .ToListAsync();

            var universities = allUniversities
                .Where(u => NormalizeText(u, true).Contains(normalizedQuery))
                .Take(5)
                .ToList();

            foreach (var uni in universities)
                suggestions.Add(uni);

            // Fetch all academic years first, then filter in memory
            var allAcademicYears = await _context.ErasmusProgrami
                .Select(p => p.AkademskaGodina)
                .Distinct()
                .ToListAsync();

            var academicYears = allAcademicYears
                .Where(y => NormalizeText(y).Contains(normalizedQuery))
                .Take(3)
                .ToList();

            foreach (var year in academicYears)
                suggestions.Add(year);

            // Add student names
            if (User.IsInRole("Admin") || User.IsInRole("ECTSKoordinator"))
            {
                // Fetch eligible students first, then filter in memory
                var allStudents = await _context.Users
                    .Where(u => u.Uloga == "Student")
                    .Select(u => new { u.FirstName, u.LastName })
                    .ToListAsync();

                var students = allStudents
                    .Where(s =>
                        NormalizeText(s.FirstName, true).Contains(normalizedQuery) ||
                        NormalizeText(s.LastName, true).Contains(normalizedQuery))
                    .Take(3)
                    .Select(s => s.FirstName + " " + s.LastName)
                    .ToList();

                foreach (var student in students)
                    suggestions.Add(student);
            }

            return Json(suggestions.Take(10).ToList());
        }

        // Helper method to normalize text for search
        private string NormalizeText(string text, bool removeSpaces = true)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            // Convert to lowercase
            text = text.ToLowerInvariant();

            // Remove diacritical marks
            text = RemoveDiacritics(text);

            // Remove extra spaces
            text = Regex.Replace(text, @"\s+", " ").Trim();

            // Optionally remove all spaces for space-insensitive search
            if (removeSpaces)
            {
                text = text.Replace(" ", "");
            }

            return text;
        }

        // Helper method to remove diacritical marks
        private string RemoveDiacritics(string text)
        {
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    // Special handling for specific Croatian characters
                    char replaced = c;
                    switch (c)
                    {
                        case 'š': replaced = 's'; break;
                        case 'đ': replaced = 'd'; break;
                        case 'č': replaced = 'c'; break;
                        case 'ć': replaced = 'c'; break;
                        case 'ž': replaced = 'z'; break;
                    }
                    stringBuilder.Append(replaced);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
