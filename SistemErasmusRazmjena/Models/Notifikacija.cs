using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemErasmusRazmjena.Models
{
    public class Notifikacija
    {
        public int Id { get; set; }

        [ForeignKey("Korisnik")]
        public string KorisnikID { get; set; } // Change this to string
        public virtual ApplicationUser Korisnik { get; set; } // Navigation property

        public string Sadrzaj { get; set; } // Renamed from 'Poruka' to 'Sadrzaj'
        public DateTime Vrijeme { get; set; }
        public bool Procitano { get; set; }
        public DateTime Datum { get; set; }
        public int PrijavaId { get; set; }

    }
}
