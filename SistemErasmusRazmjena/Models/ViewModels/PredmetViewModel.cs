using System.ComponentModel.DataAnnotations;

namespace SistemErasmusRazmjena.Models.ViewModels
{
    public class PredmetViewModel
    {
        [Required(ErrorMessage = "Home Subject is required.")]
        public string PredmetHome { get; set; }

        [Required(ErrorMessage = "Accepting Subject is required.")]
        public string PredmetAccepting { get; set; }

        public string Status { get; set; }
        public int Id { get; set; }
    }
}
