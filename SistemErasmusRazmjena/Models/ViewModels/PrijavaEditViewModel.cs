using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SistemErasmusRazmjena.Models.ViewModels
{
    public class PrijavaEditViewModel
    {
        public int PrijavaID { get; set; }
        public int ErasmusProgramID { get; set; }
        public string StudentID { get; set; }
        public string AkademskaGodina { get; set; }
        public string Naziv { get; set; }
        public string Semestar { get; set; }
        public DokumentacijaOptions DokumentacijaOptions { get; set; }
        public List<Predmet> PrijedlogPredmeta { get; set; }
        public bool IsReapplication { get; set; }
    }
}
