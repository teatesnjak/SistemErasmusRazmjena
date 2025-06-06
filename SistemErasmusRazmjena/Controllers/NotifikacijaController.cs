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
        /// Returns all notifications for the currently logged-in user.
        /// </summary>
        [HttpGet("api/notifikacije/moje")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<Notifikacija>>> GetNotifikacije()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var notifikacije = await _context.Notifikacije
                .Where(n => n.KorisnikID == user.Id)
                .OrderByDescending(n => n.Datum)
                .ToListAsync();

            return Ok(notifikacije);
        }

        /// <summary>
        /// Returns a view for the user's notifications.
        /// </summary>
        [HttpGet("notifikacije/moje-view")]
        [Authorize]
        public async Task<IActionResult> Moje()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var notifikacije = await _context.Notifikacije
                .Where(n => n.KorisnikID == user.Id)
                .OrderByDescending(n => n.Datum)
                .ToListAsync();

            // Return the Razor view with the notifications
            return new ViewResult
            {
                ViewName = "Moje",
                ViewData = new Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary<IEnumerable<Notifikacija>>(
                    new Microsoft.AspNetCore.Mvc.ModelBinding.EmptyModelMetadataProvider(),
                    new Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary())
                {
                    Model = notifikacije
                }
            };
        }

        /// <summary>
        /// Creates a new notification for a specific application.
        /// </summary>
        [HttpPost("create")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] NotifikacijaRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Sadrzaj))
            {
                return BadRequest(new { Error = "Sadržaj je obavezan." });
            }

            var prijava = await _context.Prijave
                .Include(p => p.Student)
                .FirstOrDefaultAsync(p => p.ID == request.PrijavaId);

            if (prijava == null || prijava.Student == null)
                return NotFound(new { Error = "Prijava ili student nije pronađen." });

            var notifikacija = new Notifikacija
            {
                KorisnikID = prijava.Student.Id,
                Sadrzaj = request.Sadrzaj,
                Vrijeme = DateTime.Now,
                Datum = DateTime.Now,
                Procitano = false,
                PrijavaId = request.PrijavaId // Link the notification to the application
            };

            _context.Notifikacije.Add(notifikacija);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Notifikacija je kreirana.", PrijavaId = request.PrijavaId });
        }

        public class NotifikacijaRequest
        {
            public int PrijavaId { get; set; }
            public string Sadrzaj { get; set; }
        }

        /// <summary>
        /// Marks a notification as read.
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

        /// <summary>
        /// Returns a view for the user's notifications (if needed for Razor Pages).
        /// </summary>
        [HttpGet("moje-view")]
        [Authorize]
        public async Task<IActionResult> MojeView()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var notifikacije = await _context.Notifikacije
                .Where(n => n.KorisnikID == user.Id)
                .OrderByDescending(n => n.Datum)
                .ToListAsync();

            // Return a Razor view if needed
            return Ok(notifikacije);
        }
    }
}
