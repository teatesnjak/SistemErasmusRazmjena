using System.ComponentModel.DataAnnotations.Schema;

namespace SistemErasmusRazmjena.Models
{
    public class Notifikacija
    {
        public int ID { get; set; }

        [ForeignKey("Korisnik")]
        public string KorisnikID { get; set; }
        public virtual ApplicationUser Korisnik { get; set; }

        public DateTime Vrijeme { get; set; } = DateTime.Now;
        public string Sadrzaj { get; set; }
        public bool Procitano { get; set; } = false;

    }
}
