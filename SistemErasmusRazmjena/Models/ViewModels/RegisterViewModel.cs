using SistemErasmusRazmjena.Models;
using System.ComponentModel.DataAnnotations;
namespace SistemErasmusRazmjena.Models.ViewModels
{
    public class RegisterViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        [Required]
        public string Role { get; set; }

        [Required]
        public int FakultetID { get; set; }

        public List<Fakultet> Fakulteti { get; set; } = new List<Fakultet>();
        public string FirstName { get; set; } // Added FirstName property
        public string LastName { get; set; }  // Added LastName property
    }
}
