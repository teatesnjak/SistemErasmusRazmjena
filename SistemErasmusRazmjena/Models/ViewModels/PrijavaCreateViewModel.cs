namespace SistemErasmusRazmjena.Models.ViewModels
{
    using System.ComponentModel.DataAnnotations;

    public class PrijavaCreateViewModel
    {
        public int ErasmusProgramID { get; set; }
        public string StudentID { get; set; }
        public string? AkademskaGodina { get; set; }
        public string? Naziv { get; set; }
        public string? Semestar { get; set; }
        public string? Opis { get; set; }
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

