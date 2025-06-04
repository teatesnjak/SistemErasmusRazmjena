using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering; // Add this namespace
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SistemErasmusRazmjena.Data;
using SistemErasmusRazmjena.Models;
using SistemErasmusRazmjena.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
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
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    _logger.LogWarning("Current user not found in Index action");
                    return Forbid();
                }

                var prijaveQuery = _context.Prijave
                    .Include(p => p.Student)
                    .Include(p => p.ErasmusProgram)
                    .Include(p => p.Dokumentacija)
                    .Include(p => p.PrijedlogPredmeta)
                        .ThenInclude(pp => pp.Rows)
                    .AsQueryable();

                // Apply filtering based on role
                if (!User.IsInRole("Admin"))
                {
                    if (currentUser.FakultetID.HasValue)
                    {
                        prijaveQuery = prijaveQuery.Where(p => p.Student.FakultetID == currentUser.FakultetID.Value);
                    }
                    else
                    {
                        _logger.LogWarning("ECTSKoordinator user {UserId} has no FakultetID assigned", currentUser.Id);
                        // Return empty results if coordinator has no fakultet assigned
                        prijaveQuery = prijaveQuery.Where(p => false);
                    }
                }

                var prijave = await prijaveQuery
                    .OrderByDescending(p => p.DateCreated)
                    .ToListAsync();

                _logger.LogInformation("Loaded {Count} applications for user {UserId} with role {Role}",
                    prijave.Count, currentUser.Id, User.IsInRole("Admin") ? "Admin" : "ECTSKoordinator");

                var viewModel = new PrijavaSegmentedViewModel
                {
                    UTOKU = prijave.Where(p => p.Status == StatusPrijaveAlias.UTOKU).ToList(),
                    USPJESNA = prijave.Where(p => p.Status == StatusPrijaveAlias.USPJESNA).ToList(),
                    NEUSPJESNA = prijave.Where(p => p.Status == StatusPrijaveAlias.NEUSPJESNA).ToList()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Index action: {Message}", ex.Message);
                TempData["ErrorMessage"] = "An error occurred while loading applications.";
                return View(new PrijavaSegmentedViewModel());
            }
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

        [HttpGet]
        [Authorize(Roles = "ECTSKoordinator")]
        public async Task<IActionResult> ManageSubjects(int id)
        {
            try
            {
                if (id <= 0)
                {
                    _logger.LogWarning("Invalid application ID received: {Id}", id);
                    return BadRequest("Invalid application ID.");
                }

                var prijava = await _context.Prijave
                    .Include(p => p.Student)
                    .Include(p => p.ErasmusProgram)
                    .Include(p => p.Dokumentacija)
                    .Include(p => p.PrijedlogPredmeta)
                        .ThenInclude(pp => pp.Rows)
                    .FirstOrDefaultAsync(p => p.ID == id);

                if (prijava == null)
                {
                    _logger.LogWarning("Application with ID {Id} not found", id);
                    return NotFound("Application not found.");
                }

                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    _logger.LogWarning("Current user not found in ManageSubjects action");
                    return Forbid();
                }

                // Authorization check based on user role
                if (!User.IsInRole("ECTSKoordinator") || (currentUser.FakultetID.HasValue && prijava.Student?.FakultetID != currentUser.FakultetID.Value))
                {
                    _logger.LogWarning("User {UserId} attempted to access application {ApplicationId} without authorization",
                        currentUser.Id, id);
                    return Forbid();
                }

                // Create view model with null checks
                var viewModel = new PrijavaViewModel
                {
                    PrijavaID = prijava.ID,
                    ErasmusProgramID = prijava.ErasmusProgramID,
                    StudentID = prijava.StudentID,
                    StudentName = prijava.Student?.UserName ?? prijava.Student?.Email ?? "Unknown Student",
                    Naziv = prijava.ErasmusProgram?.Univerzitet ?? "Unknown University",
                    AkademskaGodina = prijava.ErasmusProgram?.AkademskaGodina ?? "Unknown Year",
                    Semestar = prijava.ErasmusProgram?.Semestar.ToString() ?? "Unknown Semester",
                    DateAdded = prijava.ErasmusProgram?.DateAdded ?? DateTime.MinValue,
                    Opis = prijava.ErasmusProgram?.Opis ?? "No description available",
                    Univerzitet = prijava.ErasmusProgram?.Univerzitet ?? "Unknown University",
                    Status = prijava.Status,
                    DokumentacijaOptions = new DokumentacijaOptionsViewModel
                    {
                        CV = prijava.Dokumentacija?.CV ?? false,
                        MotivacionoPismo = prijava.Dokumentacija?.MotivacionoPismo ?? false,
                        UgovorOUcenju = prijava.Dokumentacija?.UgovorOUcenju ?? false
                    },
                    Predmeti = prijava.PrijedlogPredmeta?.Rows?.Select(r => new PredmetViewModel
                    {
                        Id = r.PredmetID,
                        PredmetHome = r.PredmetHome ?? "Unknown",
                        PredmetAccepting = r.PredmetAccepting ?? "Unknown",
                        Status = r.Status.ToString()
                    }).ToList() ?? new List<PredmetViewModel>()
                };

                _logger.LogInformation("Successfully loaded ManageSubjects view for application ID {Id} by user {UserId}",
                    id, currentUser.Id);

                return View("ManageSubjects", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ManageSubjects action for ID {Id}: {Message}", id, ex.Message);
                TempData["ErrorMessage"] = "An error occurred while loading the subjects.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [Authorize(Roles = "ECTSKoordinator")]
        public async Task<IActionResult> UpdateSubjectStatus(int predmetId, string status)
        {
            try
            {
                if (predmetId <= 0)
                {
                    _logger.LogWarning("Invalid subject ID received: {PredmetId}", predmetId);
                    return BadRequest("Invalid subject ID.");
                }

                var predmet = await _context.Predmeti.FirstOrDefaultAsync(p => p.PredmetID == predmetId);
                if (predmet == null)
                {
                    _logger.LogWarning("Subject with ID {PredmetId} not found", predmetId);
                    return NotFound("Subject not found.");
                }

                // Update the status
                if (Enum.TryParse(status, out StatusPredmeta newStatus))
                {
                    predmet.Status = newStatus;
                    _context.Update(predmet);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Subject status updated successfully.";
                    return RedirectToAction(nameof(ManageSubjects), new { id = predmet.PrijedlogPredmetaID });
                }
                else
                {
                    _logger.LogWarning("Invalid status value received: {Status}", status);
                    return BadRequest("Invalid status value.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while updating subject status: {Message}", ex.Message);
                TempData["ErrorMessage"] = "An error occurred while updating the subject status.";
                return RedirectToAction(nameof(ManageSubjects), new { id = predmetId });
            }
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
                .OrderByDescending(p => p.DateCreated)
                .ToListAsync();

            var viewModel = new PrijavaSegmentedViewModel
            {
                UTOKU = prijave.Where(p => p.Status == StatusPrijave.UTOKU).ToList(),
                USPJESNA = prijave.Where(p => p.Status == StatusPrijave.USPJESNA).ToList(),
                NEUSPJESNA = prijave.Where(p => p.Status == StatusPrijave.NEUSPJESNA).ToList()
            };

            return View(viewModel);
        }
        // Replace your existing Create and Details methods with these:

        [HttpGet("Create/{programId:int}")]  // Constraint: programId must be an integer
        public async Task<IActionResult> Create(int programId)  // Changed to int parameter
        {
            if (programId <= 0)
            {
                return BadRequest("Invalid program ID.");
            }

            // Get the current user
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Forbid();
            }

            // Check if the student has already applied for this program
            var existingApplication = await _context.Prijave
                .FirstOrDefaultAsync(p => p.StudentID == currentUser.Id && p.ErasmusProgramID == programId);

            if (existingApplication != null)
            {
                TempData["Info"] = "You already have an application for this program.";
                return RedirectToAction(nameof(Details), new { id = existingApplication.ID });
            }

            // Get the Erasmus program details
            var erasmusProgram = await _context.ErasmusProgrami
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ID == programId);

            if (erasmusProgram == null)
            {
                return NotFound("Program not found.");
            }

            // Create the view model with program details
            var viewModel = new PrijavaCreateViewModel
            {
                ErasmusProgramID = programId,
                StudentID = currentUser.Id,  // Add this line
                Naziv = erasmusProgram.Univerzitet,
                AkademskaGodina = erasmusProgram.AkademskaGodina,
                Semestar = erasmusProgram.Semestar.ToString(),
                Opis = erasmusProgram.Opis,
                Univerzitet = erasmusProgram.Univerzitet,
                DateAdded = erasmusProgram.DateAdded,
                DokumentacijaOptions = new DokumentacijaOptions(),
                PrijedlogPredmeta = new List<Predmet>
        {
            new Predmet() // Initialize with one empty subject row
        }
            };

            // Set StudentID separately if needed (make sure your ViewModel property is string type)

            return View(viewModel);
        }

        [HttpPost("Create/{programId:int}")]  // Add this line
        [Authorize(Roles = "Student")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PrijavaCreateViewModel model)
        {
            if (model == null)
            {
                _logger.LogWarning("Model binding failed - received null model");
                return View(new PrijavaCreateViewModel());
            }

            // Get the current user's ID from the authentication context
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId))
            {
                _logger.LogError("Unable to get current user ID");
                ModelState.AddModelError("", "Unable to identify the current user. Please log out and log back in.");
                return View(model);
            }

            // Verify the user exists in the database
            var userExists = await _context.Users.AnyAsync(u => u.Id == currentUserId);
            if (!userExists)
            {
                _logger.LogError("Current user ID {UserId} not found in database", currentUserId);
                ModelState.AddModelError("", "User account not found. Please contact support.");
                return View(model);
            }

            // Debug logging to see what's being received
            _logger.LogInformation("POST received: ErasmusProgramID={0}, CurrentUserId={1}, Naziv={2}, AkademskaGodina={3}, Semestar={4}, Opis={5}, Univerzitet={6}, DateAdded={7}",
                model.ErasmusProgramID,
                currentUserId,
                model.Naziv ?? "NULL",
                model.AkademskaGodina ?? "NULL",
                model.Semestar ?? "NULL",
                model.Opis ?? "NULL",
                model.Univerzitet ?? "NULL",
                model.DateAdded);

            // Validate required fields
            if (model.ErasmusProgramID <= 0)
            {
                ModelState.AddModelError(nameof(model.ErasmusProgramID), "Erasmus Program ID is required.");
            }

            if (string.IsNullOrEmpty(model.Naziv))
            {
                ModelState.AddModelError(nameof(model.Naziv), "The Naziv field is required.");
            }

            if (string.IsNullOrEmpty(model.AkademskaGodina))
            {
                ModelState.AddModelError(nameof(model.AkademskaGodina), "The AkademskaGodina field is required.");
            }

            if (string.IsNullOrEmpty(model.Semestar))
            {
                ModelState.AddModelError(nameof(model.Semestar), "The Semestar field is required.");
            }

            if (model.DateAdded == default(DateTime))
            {
                ModelState.AddModelError(nameof(model.DateAdded), "The DateAdded field is required.");
            }

            if (string.IsNullOrEmpty(model.Opis))
            {
                ModelState.AddModelError(nameof(model.Opis), "The Opis field is required.");
            }

            if (string.IsNullOrEmpty(model.Univerzitet))
            {
                ModelState.AddModelError(nameof(model.Univerzitet), "The Univerzitet field is required.");
            }

            if (model.DokumentacijaOptions == null)
            {
                ModelState.AddModelError(nameof(model.DokumentacijaOptions), "The DokumentacijaOptions field is required.");
            }

            // Filter out empty subjects before validation
            if (model.PrijedlogPredmeta != null)
            {
                model.PrijedlogPredmeta = model.PrijedlogPredmeta
                    .Where(p => !string.IsNullOrEmpty(p.PredmetHome) && !string.IsNullOrEmpty(p.PredmetAccepting))
                    .ToList();
            }

            if (model.PrijedlogPredmeta == null || !model.PrijedlogPredmeta.Any())
            {
                ModelState.AddModelError(nameof(model.PrijedlogPredmeta), "At least one subject must be provided.");
                return View(model);
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Model state is invalid");
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    _logger.LogWarning("Validation error: {ErrorMessage}", error.ErrorMessage);
                }

                // Re-fetch the program data if validation fails
                if (model.ErasmusProgramID > 0)
                {
                    var erasmusProgram = await _context.ErasmusProgrami
                        .AsNoTracking()
                        .FirstOrDefaultAsync(p => p.ID == model.ErasmusProgramID);

                    if (erasmusProgram != null)
                    {
                        model.AkademskaGodina = erasmusProgram.AkademskaGodina;
                        model.Naziv = erasmusProgram.Univerzitet;
                        model.Semestar = erasmusProgram.Semestar.ToString();
                        model.Opis = erasmusProgram.Opis;
                        model.Univerzitet = erasmusProgram.Univerzitet;
                        model.DateAdded = erasmusProgram.DateAdded;
                    }
                }

                // Initialize empty subjects list if null to prevent view errors
                model.PrijedlogPredmeta ??= new List<Predmet> { new Predmet() };
                model.DokumentacijaOptions ??= new DokumentacijaOptions();

                return View(model);
            }

            // Check if the student has already applied - use currentUserId instead of model.StudentID
            var existingApplication = await _context.Prijave
                .FirstOrDefaultAsync(p => p.StudentID == currentUserId &&
                                          p.ErasmusProgramID == model.ErasmusProgramID);

            if (existingApplication != null)
            {
                TempData["Info"] = "You already have an application for this program.";
                return RedirectToAction(nameof(Details), new { id = existingApplication.ID });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Ensure we have valid data
                model.DokumentacijaOptions ??= new DokumentacijaOptions();

                // Create a new Prijava object - use currentUserId instead of model.StudentID
                var prijava = new Prijava
                {
                    ErasmusProgramID = model.ErasmusProgramID,
                    StudentID = currentUserId, // Use the current user's ID from authentication
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
                            PredmetHome = p.PredmetHome,
                            PredmetAccepting = p.PredmetAccepting,
                            Status = StatusPredmeta.NACEKANJU
                        }).ToList()
                    },
                    Status = StatusPrijave.UTOKU,
                    DateCreated = DateTime.Now
                };

                _context.Prijave.Add(prijava);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Application created successfully for user {UserId} and program {ProgramId}",
                    currentUserId, model.ErasmusProgramID);

                TempData["SuccessMessage"] = "Your application has been submitted successfully.";
                return RedirectToAction(nameof(MyApplications));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error while saving application: {Message}", ex.Message);
                ModelState.AddModelError("", $"An error occurred while submitting your application: {ex.Message}");

                // Re-populate the model for the view
                if (model.ErasmusProgramID > 0)
                {
                    var erasmusProgram = await _context.ErasmusProgrami
                        .AsNoTracking()
                        .FirstOrDefaultAsync(p => p.ID == model.ErasmusProgramID);

                    if (erasmusProgram != null)
                    {
                        model.AkademskaGodina = erasmusProgram.AkademskaGodina;
                        model.Naziv = erasmusProgram.Univerzitet;
                        model.Semestar = erasmusProgram.Semestar.ToString();
                        model.Opis = erasmusProgram.Opis;
                        model.Univerzitet = erasmusProgram.Univerzitet;
                        model.DateAdded = erasmusProgram.DateAdded;
                    }
                }

                model.PrijedlogPredmeta ??= new List<Predmet> { new Predmet() };
                model.DokumentacijaOptions ??= new DokumentacijaOptions();

                return View(model);
            }
        }

        [HttpGet("Details/{id:int}")]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                if (id <= 0)
                {
                    _logger.LogWarning("Invalid application ID received: {Id}", id);
                    return BadRequest("Invalid application ID.");
                }

                var prijava = await _context.Prijave
                    .Include(p => p.Student)
                    .Include(p => p.ErasmusProgram)
                    .Include(p => p.Dokumentacija)
                    .Include(p => p.PrijedlogPredmeta)
                        .ThenInclude(pp => pp.Rows)
                    .FirstOrDefaultAsync(p => p.ID == id);

                if (prijava == null)
                {
                    _logger.LogWarning("Application with ID {Id} not found", id);
                    return NotFound("Application not found.");
                }

                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    _logger.LogWarning("Current user not found in Details action");
                    return Forbid();
                }

                // Authorization check based on user role
                bool isAuthorized = false;

                if (User.IsInRole("Admin"))
                {
                    isAuthorized = true;
                }
                else if (User.IsInRole("ECTSKoordinator"))
                {
                    // Coordinator can view applications from their fakultet
                    isAuthorized = currentUser.FakultetID.HasValue &&
                                  prijava.Student?.FakultetID == currentUser.FakultetID.Value;
                }
                else if (User.IsInRole("Student"))
                {
                    // Student can only view their own applications
                    isAuthorized = prijava.StudentID == currentUser.Id;
                }

                if (!isAuthorized)
                {
                    _logger.LogWarning("User {UserId} attempted to access application {ApplicationId} without authorization",
                        currentUser.Id, id);
                    return Forbid();
                }

                // Create view model with null checks
                var viewModel = new PrijavaViewModel
                {
                    PrijavaID = prijava.ID,
                    ErasmusProgramID = prijava.ErasmusProgramID,
                    StudentID = prijava.StudentID,
                    StudentName = prijava.Student?.UserName ?? prijava.Student?.Email ?? "Unknown Student",
                    Naziv = prijava.ErasmusProgram?.Univerzitet ?? "Unknown University",
                    AkademskaGodina = prijava.ErasmusProgram?.AkademskaGodina ?? "Unknown Year",
                    Semestar = prijava.ErasmusProgram?.Semestar.ToString() ?? "Unknown Semester",
                    DateAdded = prijava.ErasmusProgram?.DateAdded ?? DateTime.MinValue,
                    Opis = prijava.ErasmusProgram?.Opis ?? "No description available",
                    Univerzitet = prijava.ErasmusProgram?.Univerzitet ?? "Unknown University",
                    Status = prijava.Status,
                    DokumentacijaOptions = new DokumentacijaOptionsViewModel
                    {
                        CV = prijava.Dokumentacija?.CV ?? false,
                        MotivacionoPismo = prijava.Dokumentacija?.MotivacionoPismo ?? false,
                        UgovorOUcenju = prijava.Dokumentacija?.UgovorOUcenju ?? false
                    },
                    Predmeti = prijava.PrijedlogPredmeta?.Rows?.Select(r => new PredmetViewModel
                    {
                        Id = r.PredmetID,
                        PredmetHome = r.PredmetHome ?? "Unknown",
                        PredmetAccepting = r.PredmetAccepting ?? "Unknown",
                        Status = r.Status.ToString()
                    }).ToList() ?? new List<PredmetViewModel>(),
                    HasAlreadyApplied = await _context.Prijave
                        .AnyAsync(p => p.StudentID == prijava.StudentID &&
                                      p.ErasmusProgramID == prijava.ErasmusProgramID &&
                                      p.ID != prijava.ID),
                    // Add properties for admin/coordinator actions

                };

                _logger.LogInformation("Successfully loaded application details for ID {Id} by user {UserId}",
                    id, currentUser.Id);

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Details action for ID {Id}: {Message}", id, ex.Message);
                TempData["ErrorMessage"] = "An error occurred while loading the application details.";
                return RedirectToAction(nameof(Index));
            }
        }


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