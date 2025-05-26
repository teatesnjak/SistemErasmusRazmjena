using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SistemErasmusRazmjena.Models.ViewModels
{
    public class PrijavaCreateViewModel
    {
        public int ErasmusProgramID { get; set; }
        public string ProgramName { get; set; }

        // Display-only properties (no validation required)
        public string AkademskaGodina { get; set; }
        public string Semestar { get; set; }

        public List<PredmetViewModel> Predmeti { get; set; } = new List<PredmetViewModel>();
        public bool CV { get; set; }
        public bool MotivacionoPismo { get; set; }
        public bool UgovorOUcenju { get; set; }
        public bool AlreadyApplied { get; set; }
        public string StudentID { get; set; }
    }

}
