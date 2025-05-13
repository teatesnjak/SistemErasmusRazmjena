using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using SistemErasmusRazmjena.Data;
using SistemErasmusRazmjena.Models;
using System.Linq;
using System.Threading.Tasks;

namespace SistemErasmusRazmjena.Controllers
{
    [Authorize(Roles = "Student,ECTSKoordinator,Admin")]
    public class PrijavaController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public PrijavaController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        [Route("[controller]/[action]")]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            IQueryable<Prijava> prijaveQuery = _context.Prijave
                .Include(p => p.ErasmusProgram)
                .Include(p => p.Dokumentacija)
                .Include(p => p.PrijedlogPredmeta)
                .ThenInclude(pp => pp.Rows)
                .Include(p => p.Student);

            if (User.IsInRole("Admin"))
            {
                // Administrator gets all prijave
                prijaveQuery = prijaveQuery;
            }
            else if (User.IsInRole("ECTSKoordinator"))
            {
                // ECTS Koordinator gets prijave where FakultetID matches
                prijaveQuery = prijaveQuery.Where(p => p.Student.FakultetID == user.FakultetID);
            }
            else if (User.IsInRole("Student"))
            {
                // Student gets only their own prijave
                prijaveQuery = prijaveQuery.Where(p => p.StudentID == user.Id);
            }

            var prijave = await prijaveQuery.ToListAsync();
            return View(prijave);
        }

        [HttpGet]
        [Route("[controller]/[action]")]
        public async Task<IActionResult> Create()
        {
            ViewData["ErasmusProgramID"] = new SelectList(_context.ErasmusProgrami, "ID", "NazivPrograma");
            return View();
        }

        [HttpPost]
        [Route("[controller]/[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Prijava prijava)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            if (ModelState.IsValid)
            {
                prijava.StudentID = user.Id;

                var dokumentacija = new Dokumentacija { };
                var prijedlogPredmeta = new PrijedlogPredmeta { };

                _context.Dokumentacije.Add(dokumentacija);
                _context.PrijedloziPredmeta.Add(prijedlogPredmeta);
                await _context.SaveChangesAsync();

                prijava.DokumentacijaID = dokumentacija.DokumentacijaID;
                prijava.PrijedlogPredmetaID = prijedlogPredmeta.ID;
                prijava.Status = StatusPrijave.UTOKU;

                _context.Prijave.Add(prijava);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            ViewData["ErasmusProgramID"] = new SelectList(_context.ErasmusProgrami, "ID", "NazivPrograma", prijava.ErasmusProgramID);
            return View(prijava);
        }

        [HttpGet]
        [Route("[controller]/[action]/{id}")]
        public async Task<IActionResult> Details(int id)
        {
            var prijava = await _context.Prijave
                .Include(p => p.ErasmusProgram)
                .Include(p => p.Dokumentacija)
                .Include(p => p.PrijedlogPredmeta)
                .ThenInclude(p => p.Rows)
                .FirstOrDefaultAsync(p => p.ID == id);

            if (prijava == null)
                return NotFound();

            return View(prijava);
        }

        [HttpGet]
        [Route("[controller]/[action]/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var prijava = await _context.Prijave
                .Include(p => p.ErasmusProgram)
                .FirstOrDefaultAsync(p => p.ID == id);

            if (prijava == null)
                return NotFound();

            return View(prijava);
        }

        [HttpPost]
        [Route("[controller]/[action]/{id}")]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var prijava = await _context.Prijave.FindAsync(id);
            if (prijava != null)
            {
                _context.Prijave.Remove(prijava);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [Route("[controller]/[action]/{id}")]
        public async Task<IActionResult> GetPredmeti(int id)
        {
            var prijava = await _context.Prijave
                .Include(p => p.PrijedlogPredmeta)
                .ThenInclude(pp => pp.Rows)
                .FirstOrDefaultAsync(p => p.ID == id);

            if (prijava == null || prijava.PrijedlogPredmeta == null)
                return NotFound();

            var predmeti = prijava.PrijedlogPredmeta.Rows;
            return View(predmeti); // Or return as JSON if needed
        }
    }
}
