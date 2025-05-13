using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using SistemErasmusRazmjena.Data;
using SistemErasmusRazmjena.Models;

namespace SistemErasmusRazmjena.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ErasmusProgramController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ErasmusProgramController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/ErasmusProgram
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll()
        {
            var list = await _context.ErasmusProgrami.ToListAsync();
            return Ok(list);
        }

        // GET: api/ErasmusProgram/5
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> Get(int id)
        {
            var program = await _context.ErasmusProgrami.FindAsync(id);
            if (program == null)
                return NotFound();

            return Ok(program);
        }

        // POST: api/ErasmusProgram
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] ErasmusProgram model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            _context.ErasmusProgrami.Add(model);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = model.ID }, model);
        }

        // PUT: api/ErasmusProgram/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] ErasmusProgram model)
        {
            if (id != model.ID)
                return BadRequest("ID mismatch");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            _context.Entry(model).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.ErasmusProgrami.Any(e => e.ID == id))
                    return NotFound();
                throw;
            }

            return NoContent();
        }

        // DELETE: api/ErasmusProgram/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var program = await _context.ErasmusProgrami.FindAsync(id);
            if (program == null)
                return NotFound();

            _context.ErasmusProgrami.Remove(program);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
