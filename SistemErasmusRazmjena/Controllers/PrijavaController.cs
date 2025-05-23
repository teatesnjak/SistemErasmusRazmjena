using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SistemErasmusRazmjena.Data;
using SistemErasmusRazmjena.Models;
using SistemErasmusRazmjena.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SistemErasmusRazmjena.Controllers
{
    [Authorize]
    public class PrijavaController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public PrijavaController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Prijava
        [Authorize(Roles = "Admin,ECTSKoordinator")]
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                return Forbid();
            }

            if (User.IsInRole("Admin"))
            {
                var prijave = await _context.Prijave
                    .Include(p => p.Student)
                    .Include(p => p.ErasmusProgram)
                    .ToListAsync();

                return View(prijave);
            }
            else if (User.IsInRole("ECTSKoordinator"))
            {
                var prijave = await _context.Prijave
                    .Include(p => p.Student)
                    .Include(p => p.ErasmusProgram)
                    .Where(p => p.Student != null && p.Student.FakultetID == currentUser.FakultetID)
                    .ToListAsync();

                return View(prijave);
            }

            return Forbid();
        }

        [HttpPost]
        [Authorize(Roles = "Admin,ECTSKoordinator")]
        public async Task<IActionResult> UpdateStatus(int id, StatusPrijave status)
        {
            var prijava = await _context.Prijave.FindAsync(id);

            if (prijava == null)
            {
                return NotFound();
            }

            prijava.Status = status;

            _context.Update(prijava);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Application status updated successfully.";
            return RedirectToAction(nameof(Details), new { id = prijava.ID });
        }

        [HttpPost]
        [Authorize(Roles = "ECTSKoordinator")]
        public async Task<IActionResult> UpdateSubjectStatus(int predmetId, string status)
        {
            var predmet = await _context.Predmeti.FindAsync(predmetId);

            if (predmet == null)
            {
                return NotFound();
            }

            if (status == "ODOBRENO")
            {
                predmet.Status = StatusPredmeta.ODOBRENO;
            }
            else if (status == "ODBIJENO")
            {
                predmet.Status = StatusPredmeta.ODBIJENO;
            }
            else
            {
                return BadRequest("Invalid status value.");
            }

            _context.Update(predmet);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Subject status updated successfully.";
            return RedirectToAction(nameof(Details), new { id = predmet.PrijedlogPredmetaID });
        }


        // GET: Prijava/MyApplications
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> MyApplications()
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                return Forbid();
            }

            var prijave = await _context.Prijave
                .Include(p => p.ErasmusProgram)
                .Where(p => p.StudentID == currentUser.Id)
                .ToListAsync();

            return View(prijave);
        }

        // GET: Prijava/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var prijava = await _context.Prijave
                .Include(p => p.Student)
                .Include(p => p.ErasmusProgram)
                .Include(p => p.Dokumentacija)
                .Include(p => p.PrijedlogPredmeta)
                .ThenInclude(pp => pp.Rows)
                .FirstOrDefaultAsync(m => m.ID == id);

            if (prijava == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                return Forbid();
            }

            if (User.IsInRole("Admin"))
            {
                return View(prijava);
            }
            else if (User.IsInRole("ECTSKoordinator"))
            {
                if (prijava.Student == null || prijava.Student.FakultetID != currentUser.FakultetID)
                {
                    return Forbid();
                }

                return View("ManageSubjects", prijava.PrijedlogPredmeta);
            }

            if (prijava.StudentID != currentUser.Id)
            {
                return Forbid();
            }

            return View(prijava);
        }

        // GET: Prijava/Create
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Create(int programId)
        {
            var program = await _context.ErasmusProgrami.FindAsync(programId);
            if (program == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                return Forbid();
            }

            var existingApplication = await _context.Prijave
                .FirstOrDefaultAsync(p => p.StudentID == currentUser.Id && p.ErasmusProgramID == programId);

            var viewModel = new PrijavaCreateViewModel
            {
                ErasmusProgramID = programId,
                ProgramName = program.Univerzitet,
                AkademskaGodina = program.AkademskaGodina,
                Semestar = program.Semestar,
                AlreadyApplied = existingApplication != null // Set the flag
            };

            return View(viewModel);
        }

        // POST: Prijava/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Create(PrijavaCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                var currentUser = await _userManager.GetUserAsync(User);

                if (currentUser == null)
                {
                    return Forbid();
                }

                var prijedlogPredmeta = new PrijedlogPredmeta
                {
                    VrijemeIzmjene = DateTime.Now,
                    Rows = new List<Predmet>()
                };

                if (model.Predmeti != null && model.Predmeti.Count > 0)
                {
                    foreach (var predmet in model.Predmeti)
                    {
                        prijedlogPredmeta.Rows.Add(new Predmet
                        {
                            PredmetHome = predmet.PredmetHome,
                            PredmetAccepting = predmet.PredmetAccepting,
                            Status = StatusPredmeta.NACEKANJU
                        });
                    }
                }

                var dokumentacija = new Dokumentacija
                {
                    CV = model.CV,
                    MotivacionoPismo = model.MotivacionoPismo,
                    UgovorOUcenju = model.UgovorOUcenju
                };

                _context.PrijedloziPredmeta.Add(prijedlogPredmeta);
                _context.Dokumentacije.Add(dokumentacija);
                await _context.SaveChangesAsync();

                var prijava = new Prijava
                {
                    StudentID = currentUser.Id,
                    ErasmusProgramID = model.ErasmusProgramID,
                    DokumentacijaID = dokumentacija.ID,
                    PrijedlogPredmetaID = prijedlogPredmeta.ID,
                    Status = StatusPrijave.UTOKU
                };

                _context.Prijave.Add(prijava);
                await _context.SaveChangesAsync();

                var koordinatori = await _userManager.GetUsersInRoleAsync("ECTSKoordinator");
                foreach (var koordinator in koordinatori)
                {
                    if (koordinator.FakultetID == currentUser.FakultetID)
                    {
                        _context.Notifikacije.Add(new Notifikacija
                        {
                            KorisnikID = prijava.StudentID,
                            Sadrzaj = $"Your application status has been updated to {prijava.Status}",
                            Datum = DateTime.Now,
                            Procitano = false
                        });
                    }
                }

                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(MyApplications));
            }

            return View(model);
        }

        // POST: Prijava/Resubmit
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Resubmit(int id, string MotivacionoPismo, string CV, List<PredmetViewModel> updatedSubjects)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                return Forbid();
            }

            var prijava = await _context.Prijave
                .Include(p => p.Dokumentacija)
                .Include(p => p.PrijedlogPredmeta)
                .ThenInclude(pp => pp.Rows)
                .FirstOrDefaultAsync(p => p.ID == id && p.StudentID == currentUser.Id);

            if (prijava == null)
            {
                return NotFound();
            }

            if (prijava.Status != StatusPrijave.NEUSPJESNA)
            {
                return Forbid();
            }

            // Update documentation
            prijava.Dokumentacija.MotivacionoPismo = !string.IsNullOrEmpty(MotivacionoPismo);
            prijava.Dokumentacija.CV = !string.IsNullOrEmpty(CV);

            // Update denied subjects
            if (updatedSubjects != null && updatedSubjects.Count > 0)
            {
                foreach (var predmet in updatedSubjects)
                {
                    var existingSubject = prijava.PrijedlogPredmeta.Rows
                        .FirstOrDefault(p => p.PredmetHome == predmet.PredmetHome && p.Status == StatusPredmeta.ODBIJENO);

                    if (existingSubject != null)
                    {
                        existingSubject.PredmetAccepting = predmet.PredmetAccepting;
                        existingSubject.Status = StatusPredmeta.NACEKANJU;
                    }
                }
            }

            prijava.Status = StatusPrijave.UTOKU;

            _context.Update(prijava);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Your application has been resubmitted successfully.";
            return RedirectToAction(nameof(MyApplications));
        }

        // GET: Prijava/Edit/5
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var prijava = await _context.Prijave
                .Include(p => p.Dokumentacija)
                .Include(p => p.PrijedlogPredmeta)
                .ThenInclude(pp => pp.Rows)
                .FirstOrDefaultAsync(p => p.ID == id);

            if (prijava == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null || prijava.StudentID != currentUser.Id)
            {
                return Forbid();
            }

            return View(prijava);
        }
        
    }
}
