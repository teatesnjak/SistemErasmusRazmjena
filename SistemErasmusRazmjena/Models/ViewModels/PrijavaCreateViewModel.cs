using SistemErasmusRazmjena.Models;
using SistemErasmusRazmjena.Models.ViewModels;
using System.ComponentModel.DataAnnotations; // Add this namespace

namespace SistemErasmusRazmjena.Models.ViewModels
{
    public class PrijavaCreateViewModel
    {
        public int ErasmusProgramID { get; set; }
        public string StudentID { get; set; }

        // These fields should not be required
        public string? AkademskaGodina { get; set; }
        public string? Naziv { get; set; }
        public string? Semestar { get; set; }
        public string? Opis { get; set; }

        public DokumentacijaOptions DokumentacijaOptions { get; set; }
        public StatusPrijave Status { get; set; }
        public List<Subject> PrijedlogPredmeta { get; set; } = new List<Subject>();
    }

}

public class DokumentacijaOptions
{
    public bool CV { get; set; }
    public bool MotivacionoPismo { get; set; }
    public bool UgovorOUcenju { get; set; }
}

public class Subject
{
    public int PredmetID { get; set; }
    public string PredmetHome { get; set; }
}

