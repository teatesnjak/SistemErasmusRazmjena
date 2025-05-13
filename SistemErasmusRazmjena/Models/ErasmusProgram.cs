using System.ComponentModel.DataAnnotations;

namespace SistemErasmusRazmjena.Models
{
    public class ErasmusProgram
    {
        public int ID { get; set; }
        public int Semestar { get; set; } // 1 ili 2
        public string AkademskaGodina { get; set; }
        public string Univerzitet { get; set; }
        public string Opis { get; set; }

    }
}
