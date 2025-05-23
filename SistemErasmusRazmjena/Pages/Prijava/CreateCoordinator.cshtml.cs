using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using SistemErasmusRazmjena.Data;
using SistemErasmusRazmjena.Models.ViewModels;
using System.Collections.Generic;

namespace SistemErasmusRazmjena.Pages.Prijava
{
    public class CreateCoordinatorModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateCoordinatorModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public CreateCoordinatorViewModel ViewModel { get; set; }

        public IActionResult OnGet()
        {
            ViewModel = new CreateCoordinatorViewModel
            {
                Fakulteti = _context.Fakulteti.ToList(),
                FakultetSelectList = new SelectList(_context.Fakulteti, "Id", "Naziv")
            };
            return Page();
        }
    }
}
