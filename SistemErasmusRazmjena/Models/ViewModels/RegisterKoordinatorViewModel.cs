using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace SistemErasmusRazmjena.Models.ViewModels
{
    public class RegisterKoordinatorViewModel
    {
        [Required]
        public string FirstName { get; set; }
        [Required] 
        public string LastName { get; set; }

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
        public int? FakultetID { get; set; }

        public List<Fakultet> Fakulteti { get; set; } = new List<Fakultet>();
    }
}
