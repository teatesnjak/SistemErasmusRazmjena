using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using SistemErasmusRazmjena.Data;
using SistemErasmusRazmjena.Models;
using System.Threading.Tasks;
using System.Security.Claims;
using SistemErasmusRazmjena.Models.ViewModels;
using Microsoft.AspNetCore.Identity;


namespace SistemErasmusRazmjena.Controllers
{
    public class ErasmusProgramController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ErasmusProgramController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: ErasmusProgram
        public async Task<IActionResult> Index()
        {
            // Sort by creation date, newest first
            return View(await _context.ErasmusProgrami
                .OrderByDescending(p => p.DateAdded)
                .ToListAsync());
        }

        // GET: ErasmusProgram/AvailablePrograms
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> AvailablePrograms()
        {
            // Get current user
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                return Challenge();
            }

            // Get all programs ordered by newest first
            var programs = await _context.ErasmusProgrami
                .OrderByDescending(p => p.DateAdded)
                .ToListAsync();

            // Get programs the student has already applied to
            var appliedProgramIds = await _context.Prijave
                .Where(p => p.StudentID == currentUser.Id)
                .Select(p => p.ErasmusProgramID)
                .ToListAsync();

            // Create view models with application status
            var viewModels = programs.Select(p => new ProgramViewModel
            {
                Program = p,
                HasApplied = appliedProgramIds.Contains(p.ID)
            }).ToList();

            return View(viewModels);
        }

        // GET: ErasmusProgram/ApplicationDetails
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> ApplicationDetails(int programId)
        {
            // Get current user
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            // Get the student's application for this program
            var application = await _context.Prijave
                .Include(p => p.ErasmusProgram)
                .Include(p => p.Dokumentacija)
                .Include(p => p.PrijedlogPredmeta)
                .ThenInclude(pp => pp.Rows)
                .FirstOrDefaultAsync(p => p.StudentID == currentUser.Id && p.ErasmusProgramID == programId);

            if (application == null)
            {
                TempData["ErrorMessage"] = "Prijava nije pronađena.";
                return RedirectToAction(nameof(AvailablePrograms));
            }

            return View(application);
        }


        // POST: ErasmusProgram/Apply
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Apply(int programId)
        {
            // Get current user
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            // IMPORTANT: Check for existing application BEFORE starting transaction
            // This ensures we don't even start the process if an application exists
            var existingApplication = await _context.Prijave
                .AsNoTracking() // Use AsNoTracking for better performance on read-only query
                .AnyAsync(p => p.StudentID == currentUser.Id && p.ErasmusProgramID == programId);

            if (existingApplication)
            {
                TempData["ErrorMessage"] = "Već ste prijavljeni na ovaj program.";
                return RedirectToAction(nameof(AvailablePrograms));
            }

            // Check if the program exists
            var program = await _context.ErasmusProgrami.FindAsync(programId);
            if (program == null)
            {
                return NotFound();
            }

            // Use a transaction to prevent race conditions
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Double-check for existing applications (in case one was created between our check and transaction)
                existingApplication = await _context.Prijave
                    .AsNoTracking()
                    .AnyAsync(p => p.StudentID == currentUser.Id && p.ErasmusProgramID == programId);

                if (existingApplication)
                {
                    TempData["ErrorMessage"] = "Već ste prijavljeni na ovaj program.";
                    return RedirectToAction(nameof(AvailablePrograms));
                }

                // Create a new dokumentacija entry
                var dokumentacija = new Dokumentacija
                {
                    CV = false,
                    MotivacionoPismo = false,
                    UgovorOUcenju = false
                };
                _context.Dokumentacije.Add(dokumentacija);
                await _context.SaveChangesAsync();

                // Create a new prijedlog predmeta entry
                var prijedlogPredmeta = new PrijedlogPredmeta
                {
                    ErasmusProgramID = programId,
                    VrijemeIzmjene = DateTime.Now
                };
                _context.PrijedloziPredmeta.Add(prijedlogPredmeta);
                await _context.SaveChangesAsync();

                // Create new application
                var prijava = new Prijava
                {
                    StudentID = currentUser.Id,
                    ErasmusProgramID = programId,
                    DokumentacijaID = dokumentacija.ID,
                    PrijedlogPredmetaID = prijedlogPredmeta.ID,
                    Status = StatusPrijave.UTOKU,
                    DateCreated = DateTime.Now
                };

                // Add the application to the database
                _context.Prijave.Add(prijava);
                await _context.SaveChangesAsync();

                // Update the related entities with the new prijava ID
                prijedlogPredmeta.PrijavaID = prijava.ID;
                dokumentacija.PrijavaID = prijava.ID;
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                TempData["SuccessMessage"] = "Uspješno sačuvana prijava.";
                return RedirectToAction(nameof(AvailablePrograms));
            }
            catch (DbUpdateException ex) when (IsDuplicateKeyException(ex))
            {
                // This specifically catches unique constraint violations
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = "Već ste prijavljeni na ovaj program.";
                return RedirectToAction(nameof(AvailablePrograms));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = "Greška prilikom slanja prijave: " + ex.Message;
                return RedirectToAction(nameof(AvailablePrograms));
            }
        }

        // Helper method to determine if the exception is due to a duplicate key
        private bool IsDuplicateKeyException(DbUpdateException ex)
        {
            return ex.InnerException?.Message?.Contains("duplicate key") == true ||
                   ex.InnerException?.Message?.Contains("UNIQUE KEY") == true ||
                   ex.InnerException?.Message?.Contains("IX_Prijave_StudentID_ErasmusProgramID") == true;
        }



        // GET: ErasmusProgram/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var program = await _context.ErasmusProgrami
                .FirstOrDefaultAsync(m => m.ID == id);

            if (program == null)
            {
                return NotFound();
            }

            return View(program);
        }

        // GET: ErasmusProgram/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: ErasmusProgram/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("ID,Univerzitet,AkademskaGodina,Semestar,Opis")] ErasmusProgram erasmusProgram)
        {
            // Validate the AkademskaGodina format
            if (!string.IsNullOrWhiteSpace(erasmusProgram.AkademskaGodina))
            {
                var parts = erasmusProgram.AkademskaGodina.Split('/');
                if (parts.Length != 2 ||
                    !int.TryParse(parts[0], out int firstYear) ||
                    !int.TryParse(parts[1], out int secondYear) ||
                    secondYear - firstYear != 1)
                {
                    ModelState.AddModelError("AkademskaGodina", "Academska godina mora biti u formatu YYYY/YYYY, pri čemu druga godina mora biti tačno jedna godina nakon prve.");
                }
            }
            else
            {
                ModelState.AddModelError("AkademskaGodina", "Obavezna je akademska godina.");
            }
            if (ModelState.IsValid)
            {
                // Set creation date to current date/time
                erasmusProgram.DateAdded = DateTime.Now;

                _context.Add(erasmusProgram);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(erasmusProgram);
        }

        // GET: ErasmusProgram/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var program = await _context.ErasmusProgrami.FindAsync(id);
            if (program == null)
            {
                return NotFound();
            }
            return View(program);
        }

        // POST: ErasmusProgram/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("ID,Semestar,AkademskaGodina,Univerzitet,Opis,DateAdded")] ErasmusProgram model)
        {
            Console.WriteLine($"Edit method called with ID: {id}");

            if (id != model.ID)
            {
                Console.WriteLine("Neusklađenost ID-a između rute i modela.");
                ModelState.AddModelError("", "Neusklađenost ID-a. Molim Vas pokušajte opet.");
                return View(model);
            }

            if (!ModelState.IsValid)
            {
                Console.WriteLine("Validacija modela nije uspjela.");
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine($"Validation Error: {error.ErrorMessage}");
                }
                return View(model);
            }

            try
            {
                var existingProgram = await _context.ErasmusProgrami.AsNoTracking().FirstOrDefaultAsync(e => e.ID == id);
                if (existingProgram == null)
                {
                    Console.WriteLine("Program nije uspješno pronađen u bazi.");
                    return NotFound();
                }

                model.DateAdded = existingProgram.DateAdded;

                _context.Update(model);
                await _context.SaveChangesAsync();
                Console.WriteLine("Program uspješno sačuvan.");
            }
            catch (DbUpdateConcurrencyException ex)
            {
                Console.WriteLine($"Concurrency exception: {ex.Message}");
                if (!_context.ErasmusProgrami.Any(e => e.ID == id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            TempData["SuccessMessage"] = "Erasmus program je uspješno ažuriran.";
            return RedirectToAction(nameof(Index));
        }

        // GET: ErasmusProgram/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var program = await _context.ErasmusProgrami
                .FirstOrDefaultAsync(m => m.ID == id);

            if (program == null)
            {
                return NotFound();
            }

            return View(program);
        }

        // POST: ErasmusProgram/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var erasmusProgram = await _context.ErasmusProgrami
                .FirstOrDefaultAsync(ep => ep.ID == id);

            if (erasmusProgram != null)
            {
                // Remove related PrijedlogPredmeta entities
                var relatedPrijedloziPredmeta = await _context.PrijedloziPredmeta
                    .Where(pp => pp.ErasmusProgramID == erasmusProgram.ID)
                    .ToListAsync();
                _context.PrijedloziPredmeta.RemoveRange(relatedPrijedloziPredmeta);
                _context.ErasmusProgrami.Remove(erasmusProgram);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // API Methods
        [HttpGet("api/erasmusprogram")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll()
        {
            var list = await _context.ErasmusProgrami.ToListAsync();
            return Ok(list);
        }

        [HttpGet("api/erasmusprogram/{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> Get(int id)
        {
            var program = await _context.ErasmusProgrami.FindAsync(id);
            if (program == null)
                return NotFound();

            return Ok(program);
        }
   
    // Add this method to your ErasmusProgramController
[Authorize(Roles = "Admin")]
        public async Task<IActionResult> CleanupDuplicateApplications()
        {
            // This is an admin-only method to clean up duplicate applications

            // Find all duplicate applications (same student, same program)
            var duplicates = await _context.Prijave
                .GroupBy(p => new { p.StudentID, p.ErasmusProgramID })
                .Where(g => g.Count() > 1)
                .SelectMany(g => g.OrderBy(p => p.DateCreated).Skip(1)) // Keep the oldest one
                .ToListAsync();

            if (duplicates.Any())
            {
                foreach (var duplicate in duplicates)
                {
                    // Optional: log the duplicates being removed
                    Console.WriteLine($"Removing duplicate application: ID={duplicate.ID}, StudentID={duplicate.StudentID}, ProgramID={duplicate.ErasmusProgramID}");

                    _context.Prijave.Remove(duplicate);
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Cleaned up {duplicates.Count} duplicate applications.";
            }
            else
            {
                TempData["SuccessMessage"] = "Nisu pronađeni duplikati.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
