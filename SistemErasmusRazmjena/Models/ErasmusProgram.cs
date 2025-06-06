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
        public string AkademskaGodina { get; set; } = string.Empty;

        [Required]
        public string Univerzitet { get; set; } = string.Empty;

        public string Opis { get; set; } = string.Empty;

        public DateTime DateAdded { get; set; } // Add this property

        // Add this navigation property to fix the issue
        public virtual ICollection<Prijava> Prijave { get; set; } = new List<Prijava>();
    }
}
