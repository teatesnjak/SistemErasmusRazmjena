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
using System.Linq;

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

        // GET: Prijava
        [HttpGet]
        [Authorize(Roles = "Admin,ECTSKoordinator")]
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Forbid();

            var prijave = User.IsInRole("Admin")
                ? await _context.Prijave.Include(p => p.Student).Include(p => p.ErasmusProgram).Include(p => p.PrijedlogPredmeta).ToListAsync()
                : await _context.Prijave
                    .Include(p => p.Student)
                    .Include(p => p.ErasmusProgram)
                    .Include(p => p.PrijedlogPredmeta)
                    .Where(p => p.Student != null && p.Student.FakultetID == currentUser.FakultetID)
                    .ToListAsync();

            var viewModel = new PrijavaSegmentedViewModel
            {
                InProgress = prijave.Where(p => p.Status == StatusPrijave.UTOKU).ToList(),
                Successful = prijave.Where(p => p.Status == StatusPrijave.USPJESNA).ToList(),
                Unsuccessful = prijave.Where(p => p.Status == StatusPrijave.NEUSPJESNA).ToList()
            };

            return View(viewModel);
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
        [HttpGet]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> MyApplications()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Forbid();

            var prijave = await _context.Prijave
                .Include(p => p.ErasmusProgram)
                .Include(p => p.PrijedlogPredmeta)
                .Include(p => p.PrijedlogPredmeta.Rows)
                .Where(p => p.StudentID == currentUser.Id)
                .ToListAsync();

            var viewModel = new PrijavaSegmentedViewModel
            {
                InProgress = prijave.Where(p => p.Status == StatusPrijave.UTOKU).ToList(),
                Successful = prijave.Where(p => p.Status == StatusPrijave.USPJESNA).ToList(),
                Unsuccessful = prijave.Where(p => p.Status == StatusPrijave.NEUSPJESNA).ToList()
            };

            return View(viewModel);
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
                // Allow coordinators to view applications in progress and edit subjects
                if (prijava.Student == null || prijava.Student.FakultetID != currentUser.FakultetID)
                {
                    return Forbid();
                }

                if (prijava.Status == StatusPrijave.UTOKU)
                {
                    return View("ManageSubjects", prijava.PrijedlogPredmeta);
                }
            }

            if (prijava.StudentID != currentUser.Id)
            {
                return Forbid();
            }

            return View(prijava);
        }


        // GET: Prijava/Create
        [HttpGet]
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
                AkademskaGodina = program.AkademskaGodina, // Set for display
                Semestar = program.Semestar.ToString(),   // Set for display
                AlreadyApplied = existingApplication != null,
                StudentID = currentUser.Id
            };

            return View(viewModel);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Create(PrijavaCreateViewModel model)
        {
            _logger.LogInformation("Create action started for Erasmus Program ID: {ErasmusProgramID}", model.ErasmusProgramID);

            ModelState.Remove(nameof(model.Semestar));
            ModelState.Remove(nameof(model.AkademskaGodina));

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Model state is invalid for Erasmus Program ID: {ErasmusProgramID}. Errors: {Errors}",
                    model.ErasmusProgramID, ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return View(model);
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                _logger.LogError("Current user is null. User is not authenticated.");
                return Forbid();
            }

            _logger.LogInformation("Current user retrieved: {UserId}, {UserName}", currentUser.Id, currentUser.UserName);

            try
            {
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
                    _logger.LogInformation("Added {Count} subjects to the proposed subjects list.", model.Predmeti.Count);
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

                _logger.LogInformation("Proposed subjects and documentation saved successfully.");

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

                _logger.LogInformation("Application created successfully for Erasmus Program ID: {ErasmusProgramID} by User ID: {UserId}",
                    model.ErasmusProgramID, currentUser.Id);

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
                _logger.LogInformation("Notifications sent to coordinators for User ID: {UserId}", currentUser.Id);

                return RedirectToAction(nameof(MyApplications));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating the application for Erasmus Program ID: {ErasmusProgramID} by User ID: {UserId}",
                    model.ErasmusProgramID, currentUser.Id);
                throw;
            }
        }



        [HttpPost("Prijava/Edit/{id:int?}")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Edit(EditPrijavaViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                return Forbid();
            }

            var prijava = await _context.Prijave
                .Include(p => p.PrijedlogPredmeta)
                .ThenInclude(pp => pp.Rows)
                .FirstOrDefaultAsync(p => p.ID == model.PrijavaID && p.StudentID == currentUser.Id);

            if (prijava == null)
            {
                return NotFound();
            }

            if (prijava.Status != StatusPrijave.NEUSPJESNA)
            {
                TempData["ErrorMessage"] = "You can only edit applications that were unsuccessful.";
                return RedirectToAction(nameof(MyApplications));
            }

            // Update existing subjects
            if (model.ExistingSubjects != null)
            {
                foreach (var subject in model.ExistingSubjects)
                {
                    var existingSubject = prijava.PrijedlogPredmeta.Rows
                        .FirstOrDefault(p => p.PredmetHome == subject.PredmetHome);

                    if (existingSubject != null)
                    {
                        existingSubject.PredmetAccepting = subject.PredmetAccepting;
                    }
                }
            }

            // Add new subjects
            if (model.NewSubjects != null && model.NewSubjects.Count > 0)
            {
                foreach (var newSubject in model.NewSubjects)
                {
                    prijava.PrijedlogPredmeta.Rows.Add(new Predmet
                    {
                        PredmetHome = newSubject.PredmetHome,
                        PredmetAccepting = newSubject.PredmetAccepting,
                        Status = StatusPredmeta.NACEKANJU
                    });
                }
            }

            prijava.PrijedlogPredmeta.VrijemeIzmjene = DateTime.Now;

            _context.Update(prijava);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Your application has been updated successfully.";
            return RedirectToAction(nameof(MyApplications));
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
                TempData["ErrorMessage"] = "You can only edit applications that were unsuccessful.";
                return RedirectToAction(nameof(MyApplications));
          
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


        [HttpGet("Prijava/Edit/{id:int?}")]
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

            if (prijava.Status != StatusPrijave.NEUSPJESNA)
            {
                TempData["ErrorMessage"] = "You can only edit applications that were unsuccessful.";
                return RedirectToAction(nameof(MyApplications));
            }

            var viewModel = new EditPrijavaViewModel
            {
                PrijavaID = prijava.ID,
                Dokumentacija = new DokumentacijaViewModel
                {
                    CV = prijava.Dokumentacija.CV,
                    MotivacionoPismo = prijava.Dokumentacija.MotivacionoPismo
                },
                ExistingSubjects = prijava.PrijedlogPredmeta.Rows
                    .Select(p => new PredmetViewModel
                    {
                        PredmetHome = p.PredmetHome,
                        PredmetAccepting = p.PredmetAccepting
                    }).ToList(),
                NewSubjects = new List<PredmetViewModel>()

            };

            return View("Edit", viewModel);
        }



        [HttpGet("Prijava/EditCoordinator/{id:int}")]
        [Authorize(Roles = "ECTSKoordinator")]
        public IActionResult EditCoordinator(int id)
        {
            var prijava = _context.Prijave
                .Include(p => p.Dokumentacija)
                .Include(p => p.PrijedlogPredmeta)
                .ThenInclude(pp => pp.Rows)
                .FirstOrDefault(p => p.ID == id);

            if (prijava == null)
            {
                return NotFound();
            }

            var viewModel = new EditPrijavaViewModel
            {
                PrijavaID = prijava.ID,
                Dokumentacija = new DokumentacijaViewModel
                {
                    CV = prijava.Dokumentacija?.CV ?? false,
                    MotivacionoPismo = prijava.Dokumentacija?.MotivacionoPismo ?? false
                },
                PrijedlogPredmeta = new PrijedlogPredmetaViewModel
                {
                    Rows = prijava.PrijedlogPredmeta.Rows
                        .Select(p => new PredmetRow
                        {
                            PredmetHome = p.PredmetHome,
                            PredmetAccepting = p.PredmetAccepting
                        }).ToList()
                }
            };

            return View(viewModel);
        }


    }
}
