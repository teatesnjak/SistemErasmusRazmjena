
namespace SistemErasmusRazmjena.Models.ViewModels
{
    using System.ComponentModel.DataAnnotations;

    public class PrijavaCreateViewModel
    {
        public int ErasmusProgramID { get; set; }
        public string Naziv { get; set; } // Program name
        public string AkademskaGodina { get; set; } // Academic year
        public string? StudentID { get; set; }  // Make it nullable
        // Other properties related to the application
        public string Semestar { get; set; }
        public string Opis { get; set; }
        public string Univerzitet { get; set; } // Add this property
        public DateTime DateAdded { get; set; }
        public DokumentacijaOptions DokumentacijaOptions { get; set; }
        public StatusPrijave Status { get; set; }
        public List<Predmet> PrijedlogPredmeta { get; set; } // Changed from Subject to Predmet
    }


    public class DokumentacijaOptions
    {
        public bool CV { get; set; }
        public bool MotivacionoPismo { get; set; }
        public bool UgovorOUcenju { get; set; }
    }


}

