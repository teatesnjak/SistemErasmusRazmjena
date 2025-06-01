using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SistemErasmusRazmjena.Data;
using SistemErasmusRazmjena.Models;
using SistemErasmusRazmjena.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StatusPrijaveAlias = SistemErasmusRazmjena.Models.StatusPrijave; // Updated alias to avoid conflict

// Rest of the file remains unchanged


namespace SistemErasmusRazmjena.Controllers
{
    [Authorize]
    public class PrijavaController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<PrijavaController> _logger;

        public PrijavaController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ILogger<PrijavaController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        [HttpGet]
        [Authorize(Roles = "Admin,ECTSKoordinator")]
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Forbid();

            var prijaveQuery = _context.Prijave
                .Include(p => p.Student)
                .Include(p => p.ErasmusProgram)
                .Include(p => p.Dokumentacija)
                .Include(p => p.PrijedlogPredmeta)
                    .ThenInclude(pp => pp.Rows)
                .AsQueryable(); // <<< osigurava da je IQueryable, ne IIncludableQueryable

            if (!User.IsInRole("Admin"))
            {
                prijaveQuery = prijaveQuery.Where(p => p.Student.FakultetID == currentUser.FakultetID);
            }



            var prijave = await prijaveQuery.OrderByDescending(p => p.ErasmusProgram.DateAdded).ToListAsync();

            var viewModel = new PrijavaSegmentedViewModel
            {
                UTOKU = prijave.Where(p => p.Status == StatusPrijaveAlias.UTOKU).ToList(),
                USPJESNA = prijave.Where(p => p.Status == StatusPrijaveAlias.USPJESNA).ToList(),
                NEUSPJESNA = prijave.Where(p => p.Status == StatusPrijaveAlias.NEUSPJESNA).ToList()
            };

            return View(viewModel);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,ECTSKoordinator")]
        public async Task<IActionResult> UpdateStatus(int id, StatusPrijave status)
        {
            var prijava = await _context.Prijave.FindAsync(id);
            if (prijava == null) return NotFound();

            prijava.Status = status;
            _context.Update(prijava);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Status prijave je uspješno ažuriran.";
            return RedirectToAction(nameof(Details), new { id = prijava.ID });
        }

        [HttpPost]
        [Authorize(Roles = "ECTSKoordinator")]
        public async Task<IActionResult> UpdateSubjectStatus(int predmetId, string status)
        {
            var predmet = await _context.Predmeti.FindAsync(predmetId);
            if (predmet == null) return NotFound();

            predmet.Status = status switch
            {
                "ODOBRENO" => StatusPredmeta.ODOBRENO,
                "ODBIJENO" => StatusPredmeta.ODBIJENO,
                _ => throw new ArgumentException("Nevažeć status.")
            };

            _context.Update(predmet);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Status predmeta je uspješno ažuriran.";
            return RedirectToAction(nameof(Details), new { id = predmet.PrijedlogPredmetaID });
        }

        [HttpGet]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> MyApplications()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Forbid();

            var prijave = await _context.Prijave
                .Include(p => p.ErasmusProgram)
                .Include(p => p.PrijedlogPredmeta).ThenInclude(pp => pp.Rows)
                .Where(p => p.StudentID == currentUser.Id)
                .ToListAsync();

            var viewModel = new PrijavaSegmentedViewModel
            {
                UTOKU = prijave.Where(p => p.Status == StatusPrijave.UTOKU).ToList(),
                USPJESNA = prijave.Where(p => p.Status == StatusPrijave.USPJESNA).ToList(),
                NEUSPJESNA = prijave.Where(p => p.Status == StatusPrijave.NEUSPJESNA).ToList()
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var prijava = await _context.Prijave
                .Include(p => p.Student)
                .Include(p => p.ErasmusProgram)
                .Include(p => p.Dokumentacija)
                .Include(p => p.PrijedlogPredmeta).ThenInclude(pp => pp.Rows)
                .FirstOrDefaultAsync(p => p.ID == id);

            if (prijava == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Forbid();

            var viewModel = new PrijavaViewModel
            {
                PrijavaID = prijava.ID,
                ErasmusProgramID = prijava.ErasmusProgramID,
                StudentID = prijava.StudentID,
                StudentName = prijava.Student?.UserName ?? "Unknown",
                AkademskaGodina = prijava.ErasmusProgram?.AkademskaGodina ?? "Unknown",
                Naziv = prijava.ErasmusProgram?.Univerzitet ?? "Unknown",
                Semestar = prijava.ErasmusProgram?.Semestar.ToString() ?? "Unknown",
                Opis = prijava.ErasmusProgram?.Opis ?? "No description available",
                Status = prijava.Status,
                DokumentacijaOptions = new DokumentacijaOptionsViewModel
                {
                    CV = prijava.Dokumentacija?.CV ?? false,
                    MotivacionoPismo = prijava.Dokumentacija?.MotivacionoPismo ?? false,
                    UgovorOUcenju = prijava.Dokumentacija?.UgovorOUcenju ?? false
                },
                Predmeti = prijava.PrijedlogPredmeta.Rows.Select(r => new PredmetViewModel
                {
                    Id = r.PredmetID,
                    PredmetHome = r.PredmetHome,
                    PredmetAccepting = r.PredmetAccepting,
                    Status = r.Status.ToString()
                }).ToList()
            };

            if (User.IsInRole("Admin")) return View(viewModel);

            if (User.IsInRole("ECTSKoordinator") &&
                prijava.Student?.FakultetID == currentUser.FakultetID &&
                prijava.Status == StatusPrijaveAlias.UTOKU)
            {
                return View("ManageSubjects", viewModel);
            }

            if (prijava.StudentID != currentUser.Id) return Forbid();

            return View(viewModel);
        }


        [HttpGet]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Create(int programId)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Forbid();
            }

            // Fetch ErasmusProgram details
            var erasmusProgram = await _context.ErasmusProgrami.FindAsync(programId);
            if (erasmusProgram == null)
            {
                return NotFound();
            }

            var availableSubjects = await _context.Predmeti.ToListAsync();
            // In the Create GET method, update the PrijedlogPredmeta initialization
            var viewModel = new PrijavaCreateViewModel
            {
                ErasmusProgramID = programId,
                StudentID = user.Id,
                AkademskaGodina = erasmusProgram.AkademskaGodina,
                Naziv = erasmusProgram.Univerzitet,
                Semestar = erasmusProgram.Semestar.ToString(),
                Opis = erasmusProgram.Opis,
                DokumentacijaOptions = new DokumentacijaOptions(),
                PrijedlogPredmeta = availableSubjects.Select(s => new Predmet // Changed from Subject to Predmet
                {
                    PredmetID = s.PredmetID,
                    PredmetHome = s.PredmetHome,
                    PredmetAccepting = "", // Initialize with empty string
                    Status = StatusPredmeta.NACEKANJU
                }).ToList(),
                Status = StatusPrijave.UTOKU
            };



            return View(viewModel);
        }


        // Modify the Create method to return to the correct view with the correct model type
        [HttpPost]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Create(PrijavaCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Filter out any empty entries in PrijedlogPredmeta
                model.PrijedlogPredmeta = model.PrijedlogPredmeta
                    .Where(p => !string.IsNullOrEmpty(p.PredmetHome) && !string.IsNullOrEmpty(p.PredmetAccepting))
                    .ToList();

                if (model.PrijedlogPredmeta.Count == 0)
                {
                    ModelState.AddModelError("", "At least one subject must be provided");
                    return View(model);
                }

                var prijava = new Prijava
                {
                    ErasmusProgramID = model.ErasmusProgramID,
                    StudentID = model.StudentID,
                    Dokumentacija = new Dokumentacija
                    {
                        CV = model.DokumentacijaOptions.CV,
                        MotivacionoPismo = model.DokumentacijaOptions.MotivacionoPismo,
                        UgovorOUcenju = model.DokumentacijaOptions.UgovorOUcenju
                    },
                    PrijedlogPredmeta = new PrijedlogPredmeta
                    {
                        ErasmusProgramID = model.ErasmusProgramID,
                        VrijemeIzmjene = DateTime.Now,
                        Rows = model.PrijedlogPredmeta.Select(p => new Predmet
                        {
                            PredmetID = p.PredmetID,
                            PredmetHome = p.PredmetHome,
                            PredmetAccepting = p.PredmetAccepting, // Make sure this is set
                            Status = StatusPredmeta.NACEKANJU
                        }).ToList()
                    },
                    Status = StatusPrijave.UTOKU,
                    DateCreated = DateTime.Now
                };

                _context.Prijave.Add(prijava);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(MyApplications));
            }

            return View(model);
        }






    }
}