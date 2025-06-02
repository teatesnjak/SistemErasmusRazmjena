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


        // Add these action methods to your PrijavaController class

        [HttpGet]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Edit(int? id)
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
            if (currentUser == null || prijava.StudentID != currentUser.Id) return Forbid();

            // Only allow editing applications that are in progress
            if (prijava.Status != StatusPrijave.UTOKU)
            {
                TempData["ErrorMessage"] = "Only applications in progress can be edited.";
                return RedirectToAction(nameof(Details), new { id = prijava.ID });
            }

            var viewModel = new PrijavaEditViewModel
            {
                PrijavaID = prijava.ID,
                ErasmusProgramID = prijava.ErasmusProgramID,
                StudentID = prijava.StudentID,
                AkademskaGodina = prijava.ErasmusProgram?.AkademskaGodina ?? "",
                Naziv = prijava.ErasmusProgram?.Univerzitet ?? "",
                Semestar = prijava.ErasmusProgram?.Semestar.ToString() ?? "",
                DokumentacijaOptions = new DokumentacijaOptions
                {
                    CV = prijava.Dokumentacija?.CV ?? false,
                    MotivacionoPismo = prijava.Dokumentacija?.MotivacionoPismo ?? false,
                    UgovorOUcenju = prijava.Dokumentacija?.UgovorOUcenju ?? false
                },
                PrijedlogPredmeta = prijava.PrijedlogPredmeta.Rows.ToList()
            };

            return View(viewModel);
        }

        [HttpPost]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Edit(PrijavaEditViewModel model)
        {
            if (model == null || model.PrijavaID <= 0) return NotFound();

            // Log incoming data
            _logger.LogInformation($"Editing application {model.PrijavaID}");

            // Filter valid subjects
            model.PrijedlogPredmeta = model.PrijedlogPredmeta?
                .Where(p => !string.IsNullOrEmpty(p.PredmetHome) && !string.IsNullOrEmpty(p.PredmetAccepting))
                .ToList() ?? new List<Predmet>();

            if (model.PrijedlogPredmeta.Count == 0)
            {
                ModelState.AddModelError("", "At least one subject must be provided");
                return View(model);
            }

            // Access check
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Forbid();

            // Load minimal data needed for check
            var prijava = await _context.Prijave
                .FirstOrDefaultAsync(p => p.ID == model.PrijavaID && p.StudentID == currentUser.Id);

            if (prijava == null) return NotFound();
            if (prijava.Status != StatusPrijave.UTOKU)
            {
                TempData["ErrorMessage"] = "Only applications in progress can be edited.";
                return RedirectToAction(nameof(Details), new { id = model.PrijavaID });
            }

            // Get IDs
            var dokumentacijaId = prijava.DokumentacijaID;
            var prijedlogPredmetaId = prijava.PrijedlogPredmetaID;

            try
            {
                // DIRECT DATABASE APPROACH

                // 1. Update documentation using SQL
                string docSql = @"UPDATE Dokumentacije 
                          SET CV = @cv, MotivacionoPismo = @mp, UgovorOUcenju = @uo 
                          WHERE ID = @id";

                await _context.Database.ExecuteSqlRawAsync(docSql,
                    new Microsoft.Data.SqlClient.SqlParameter("@cv", model.DokumentacijaOptions.CV),
                    new Microsoft.Data.SqlClient.SqlParameter("@mp", model.DokumentacijaOptions.MotivacionoPismo),
                    new Microsoft.Data.SqlClient.SqlParameter("@uo", model.DokumentacijaOptions.UgovorOUcenju),
                    new Microsoft.Data.SqlClient.SqlParameter("@id", dokumentacijaId));

                _logger.LogInformation("Documentation updated");

                // 2. Get existing subjects
                var existingSubjectIds = await _context.Predmeti
                    .Where(p => p.PrijedlogPredmetaID == prijedlogPredmetaId)
                    .Select(p => p.PredmetID)
                    .ToListAsync();

                _logger.LogInformation($"Found {existingSubjectIds.Count} existing subjects");

                // 3. Determine which subjects to keep, update, or delete
                var keptSubjectIds = model.PrijedlogPredmeta
                    .Where(p => p.PredmetID > 0)
                    .Select(p => p.PredmetID)
                    .ToList();

                var subjectsToDelete = existingSubjectIds.Except(keptSubjectIds).ToList();

                // 4. Delete removed subjects
                if (subjectsToDelete.Any())
                {
                    foreach (var subjectId in subjectsToDelete)
                    {
                        await _context.Database.ExecuteSqlRawAsync(
                            "DELETE FROM Predmeti WHERE PredmetID = @id",
                            new Microsoft.Data.SqlClient.SqlParameter("@id", subjectId));

                        _logger.LogInformation($"Deleted subject {subjectId}");
                    }
                }

                // 5. Update existing subjects
                foreach (var subject in model.PrijedlogPredmeta.Where(p => p.PredmetID > 0))
                {
                    await _context.Database.ExecuteSqlRawAsync(
                        "UPDATE Predmeti SET PredmetHome = @home, PredmetAccepting = @accepting WHERE PredmetID = @id",
                        new Microsoft.Data.SqlClient.SqlParameter("@home", subject.PredmetHome),
                        new Microsoft.Data.SqlClient.SqlParameter("@accepting", subject.PredmetAccepting),
                        new Microsoft.Data.SqlClient.SqlParameter("@id", subject.PredmetID));

                    _logger.LogInformation($"Updated subject {subject.PredmetID}");
                }

                // 6. Add new subjects
                var newSubjects = model.PrijedlogPredmeta.Where(p => p.PredmetID == 0).ToList();
                _logger.LogInformation($"Adding {newSubjects.Count} new subjects");

                foreach (var subject in newSubjects)
                {
                    try
                    {
                        // Create parameter objects explicitly with correct types
                        var ppidParam = new Microsoft.Data.SqlClient.SqlParameter("@ppid", System.Data.SqlDbType.Int) 
                            { Value = prijedlogPredmetaId };
                        var homeParam = new Microsoft.Data.SqlClient.SqlParameter("@home", System.Data.SqlDbType.NVarChar, 100) 
                            { Value = subject.PredmetHome ?? "" };
                        var acceptingParam = new Microsoft.Data.SqlClient.SqlParameter("@accepting", System.Data.SqlDbType.NVarChar, 100) 
                            { Value = subject.PredmetAccepting ?? "" };
                        var statusParam = new Microsoft.Data.SqlClient.SqlParameter("@status", System.Data.SqlDbType.Int) 
                            { Value = 0 }; // 0 represents NACEKANJU in your enum
                        
                        // Execute with properly typed parameters
                        await _context.Database.ExecuteSqlRawAsync(
                            "INSERT INTO Predmeti (PrijedlogPredmetaID, PredmetHome, PredmetAccepting, Status) VALUES (@ppid, @home, @accepting, @status)",
                            ppidParam, homeParam, acceptingParam, statusParam);
                        
                        _logger.LogInformation($"Added new subject: {subject.PredmetHome} -> {subject.PredmetAccepting}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error adding subject: {Message}", ex.Message);
                        throw; // Re-throw to be caught by outer try/catch
                    }
                }

                // 7. Update last modified time
                await _context.Database.ExecuteSqlRawAsync(
                    "UPDATE PrijedloziPredmeta SET VrijemeIzmjene = @time WHERE ID = @id",
                    new Microsoft.Data.SqlClient.SqlParameter("@time", DateTime.Now),
                    new Microsoft.Data.SqlClient.SqlParameter("@id", prijedlogPredmetaId));

                TempData["SuccessMessage"] = "Your application has been successfully updated.";
                return RedirectToAction(nameof(Details), new { id = model.PrijavaID });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving application changes: {Message}", ex.Message);
                ModelState.AddModelError("", $"An error occurred while saving your changes: {ex.Message}");

                // Reload view model data for the form
                var viewModel = new PrijavaEditViewModel
                {
                    PrijavaID = model.PrijavaID,
                    ErasmusProgramID = model.ErasmusProgramID,
                    StudentID = model.StudentID,
                    AkademskaGodina = model.AkademskaGodina,
                    Naziv = model.Naziv,
                    Semestar = model.Semestar,
                    DokumentacijaOptions = model.DokumentacijaOptions,
                    PrijedlogPredmeta = model.PrijedlogPredmeta
                };

                return View(viewModel);
            }

        }

        [HttpGet]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Reapply(int? id)
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
            if (currentUser == null || prijava.StudentID != currentUser.Id) return Forbid();

            // Only allow reapplying to rejected applications
            if (prijava.Status != StatusPrijave.NEUSPJESNA)
            {
                TempData["ErrorMessage"] = "You can only reapply to rejected applications.";
                return RedirectToAction(nameof(Details), new { id = prijava.ID });
            }

            // Create view model for reapplication (similar to edit view model)
            var viewModel = new PrijavaEditViewModel
            {
                PrijavaID = prijava.ID, // Original application ID for reference
                ErasmusProgramID = prijava.ErasmusProgramID,
                StudentID = prijava.StudentID,
                AkademskaGodina = prijava.ErasmusProgram?.AkademskaGodina ?? "",
                Naziv = prijava.ErasmusProgram?.Univerzitet ?? "",
                Semestar = prijava.ErasmusProgram?.Semestar.ToString() ?? "",
                DokumentacijaOptions = new DokumentacijaOptions
                {
                    CV = prijava.Dokumentacija?.CV ?? false,
                    MotivacionoPismo = prijava.Dokumentacija?.MotivacionoPismo ?? false,
                    UgovorOUcenju = prijava.Dokumentacija?.UgovorOUcenju ?? false
                },
                PrijedlogPredmeta = prijava.PrijedlogPredmeta.Rows.Select(r => new Predmet
                {
                    PredmetID = 0, // Set to 0 as these will be new entries
                    PredmetHome = r.PredmetHome,
                    PredmetAccepting = r.PredmetAccepting,
                    Status = StatusPredmeta.NACEKANJU // Reset status to pending
                }).ToList(),
                IsReapplication = true // Flag to indicate this is a reapplication
            };

            return View("Reapply", viewModel);
        }

        [HttpPost]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Reapply(PrijavaEditViewModel model)
        {
            if (model == null) return BadRequest();

            // Validate data
            model.PrijedlogPredmeta = model.PrijedlogPredmeta?
                .Where(p => !string.IsNullOrEmpty(p.PredmetHome) && !string.IsNullOrEmpty(p.PredmetAccepting))
                .ToList() ?? new List<Predmet>();

            if (model.PrijedlogPredmeta.Count == 0)
            {
                ModelState.AddModelError("", "At least one subject must be provided");
                return View(model);
            }

            // Access check
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Forbid();

            // Load the full application with all related data
            var prijava = await _context.Prijave
                .Include(p => p.Dokumentacija)
                .Include(p => p.PrijedlogPredmeta).ThenInclude(pp => pp.Rows)
                .FirstOrDefaultAsync(p => p.ID == model.PrijavaID && p.StudentID == currentUser.Id);

            if (prijava == null) return NotFound();
            
            // Only allow reapplying to rejected applications
            if (prijava.Status != StatusPrijave.NEUSPJESNA)
            {
                TempData["ErrorMessage"] = "You can only reapply to rejected applications.";
                return RedirectToAction(nameof(Details), new { id = prijava.ID });
            }

            try
            {
                // Begin transaction to ensure data consistency
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    // 1. Update application status
                    prijava.Status = StatusPrijave.UTOKU;
                    
                    // 2. Update documentation
                    if (prijava.Dokumentacija != null)
                    {
                        prijava.Dokumentacija.CV = model.DokumentacijaOptions.CV;
                        prijava.Dokumentacija.MotivacionoPismo = model.DokumentacijaOptions.MotivacionoPismo;
                        prijava.Dokumentacija.UgovorOUcenju = model.DokumentacijaOptions.UgovorOUcenju;
                        _context.Update(prijava.Dokumentacija);
                    }
                    
                    // 3. Remove all existing subjects
                    foreach (var subject in prijava.PrijedlogPredmeta.Rows.ToList())
                    {
                        _context.Predmeti.Remove(subject);
                    }
                    prijava.PrijedlogPredmeta.Rows.Clear();
                    
                    // 4. Add new subjects
                    foreach (var subject in model.PrijedlogPredmeta)
                    {
                        var newPredmet = new Predmet
                        {
                            PrijedlogPredmetaID = prijava.PrijedlogPredmetaID,
                            PredmetHome = subject.PredmetHome,
                            PredmetAccepting = subject.PredmetAccepting,
                            Status = StatusPredmeta.NACEKANJU
                        };
                        
                        prijava.PrijedlogPredmeta.Rows.Add(newPredmet);
                    }
                    
                    // 5. Update time
                    prijava.PrijedlogPredmeta.VrijemeIzmjene = DateTime.Now;
                    
                    // Save changes
                    _context.Update(prijava);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    
                    TempData["SuccessMessage"] = "Your application has been successfully resubmitted and is now in progress.";
                    return RedirectToAction(nameof(MyApplications));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating reapplication: {Message}", ex.Message);
                ModelState.AddModelError("", $"An error occurred while updating your application: {ex.Message}");
                return View(model);
            }
        }

    }
}