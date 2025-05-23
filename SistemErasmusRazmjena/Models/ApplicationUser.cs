using Microsoft.AspNetCore.Identity;

namespace SistemErasmusRazmjena.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string Uloga { get; set; } // Student, ECTSKoordinator, Admin
        public int? FakultetID { get; set; }
        public string FirstName { get; set; } // Added property
        public string LastName { get; set; }  // Added property
    }
}
