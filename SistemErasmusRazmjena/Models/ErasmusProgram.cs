using System.ComponentModel.DataAnnotations;

namespace SistemErasmusRazmjena.Models
{
    public class ErasmusProgram
    {
        public int ID { get; set; }

        [Required]
        [Range(1, 2, ErrorMessage = "Semester must be 1 (Winter) or 2 (Summer).")]
        public int Semestar { get; set; } // 1 ili 2

        [Required]
        [RegularExpression(@"^\d{4}/\d{4}$", ErrorMessage = "Academic year must be in the format YYYY/YYYY.")]
        public string AkademskaGodina { get; set; }

        [Required]
        public string Univerzitet { get; set; }

        public string Opis { get; set; }

        public DateTime DateAdded { get; set; } // Add this property

    }
}
