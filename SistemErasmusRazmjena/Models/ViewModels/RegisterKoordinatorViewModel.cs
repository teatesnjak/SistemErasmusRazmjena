using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace SistemErasmusRazmjena.Models.ViewModels
{
    public class RegisterKoordinatorViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required]
        public int? FakultetID { get; set; }

        public List<Fakultet> Fakulteti { get; set; } = new List<Fakultet>();
    }
}
