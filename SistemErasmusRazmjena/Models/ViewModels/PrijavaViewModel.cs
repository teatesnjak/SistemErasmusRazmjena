using SistemErasmusRazmjena.Models;
using System.Collections.Generic;

namespace SistemErasmusRazmjena.Models.ViewModels
{
    public class DokumentacijaOptionsViewModel
    {
        public bool CV { get; set; }
        public bool MotivacionoPismo { get; set; }
        public bool UgovorOUcenju { get; set; }
    }

    public class PrijavaViewModel
    {
        public int PrijavaID { get; set; }
        public int ErasmusProgramID { get; set; }
        public string StudentID { get; set; } // Changed from int to string
        public required string StudentName { get; set; }
        public required string AkademskaGodina { get; set; }
        public required string Naziv { get; set; }
        public required string Semestar { get; set; }
        public required string Opis { get; set; }
        public DokumentacijaOptionsViewModel DokumentacijaOptions { get; set; }
        public List<PredmetViewModel> Predmeti { get; set; }
        public StatusPrijave Status { get; set; }
    }


    public class PrijedlogPredmetaRowViewModel
    {
        public required string PredmetHome { get; set; }
        public required string PredmetAccepting { get; set; }
    }
}
