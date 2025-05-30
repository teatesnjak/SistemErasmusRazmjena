using System.ComponentModel.DataAnnotations.Schema;

namespace SistemErasmusRazmjena.Models
{
    public class PrijedlogPredmeta
    {
        public int ID { get; set; }
        public int PrijavaID { get; set; }
        public int? ErasmusProgramID { get; set; } // Add this property
        public virtual ErasmusProgram? ErasmusProgram { get; set; } // Made nullable to fix CS8618
        public DateTime VrijemeIzmjene { get; set; } = DateTime.Now;

        public virtual List<Predmet> Rows { get; set; } = new List<Predmet>(); // Initialized to fix CS8618
        [NotMapped]
        public virtual List<Predmet> NewSubjects { get; set; } = new List<Predmet>();
    }
}
