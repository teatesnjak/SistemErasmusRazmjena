using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using SistemErasmusRazmjena.Data;
using SistemErasmusRazmjena.Models;

namespace SistemErasmusRazmjena.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class NotifikacijaController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotifikacijaController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        /// <summary>
        /// Vraća sve notifikacije za trenutno prijavljenog korisnika.
        /// </summary>
        [HttpGet("moje")]
        public async Task<ActionResult<IEnumerable<Notifikacija>>> GetNotifikacije()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var notifikacije = await _context.Notifikacije
                .Where(n => n.KorisnikID == user.Id)
                .OrderByDescending(n => n.Vrijeme)
                .ToListAsync();

            return Ok(notifikacije);
        }

        /// <summary>
        /// Oznaci notifikaciju kao procitanu.
        /// </summary>
        [HttpPut("oznaciProcitano/{id}")]
        public async Task<IActionResult> OznaciProcitano(int id)
        {
            var notifikacija = await _context.Notifikacije.FindAsync(id);
            if (notifikacija == null)
                return NotFound();

            notifikacija.Procitano = true;
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
