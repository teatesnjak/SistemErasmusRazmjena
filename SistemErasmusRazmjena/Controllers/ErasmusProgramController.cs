using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using SistemErasmusRazmjena.Data;
using SistemErasmusRazmjena.Models;
using System.Threading.Tasks;

namespace SistemErasmusRazmjena.Controllers
{
    public class ErasmusProgramController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ErasmusProgramController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: ErasmusProgram
        public async Task<IActionResult> Index()
        {
            var list = await _context.ErasmusProgrami
                .OrderByDescending(p => p.DateAdded) // Sort by DateAdded in descending order
                .ToListAsync();
            return View(list);
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
        public async Task<IActionResult> Create([Bind("Semestar,AkademskaGodina,Univerzitet,Opis,Name")] ErasmusProgram model)
        {
            if (ModelState.IsValid)
            {
                model.DateAdded = DateTime.UtcNow; // Automatically set the current date and time
                _context.Add(model);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // Log validation errors
            foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
            {
                Console.WriteLine(error.ErrorMessage);
            }

            return View(model);
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
        public async Task<IActionResult> Edit(int id, [Bind("ID,Semestar,AkademskaGodina,Univerzitet,Opis,Name")] ErasmusProgram model)
        {
            if (id != model.ID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(model);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.ErasmusProgrami.Any(e => e.ID == id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(model);
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
            var program = await _context.ErasmusProgrami.FindAsync(id);
            if (program == null)
            {
                return NotFound();
            }

            _context.ErasmusProgrami.Remove(program);
            await _context.SaveChangesAsync();
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
    }
}
