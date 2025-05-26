namespace SistemErasmusRazmjena.Models.ViewModels
{
    public class EditPrijavaViewModel
    {
        public int PrijavaID { get; set; }
        public List<PredmetViewModel> ExistingSubjects { get; set; } = new List<PredmetViewModel>();
        public List<PredmetViewModel> NewSubjects { get; set; } = new List<PredmetViewModel>();
        public DokumentacijaViewModel Dokumentacija { get; set; }
        public PrijedlogPredmetaViewModel PrijedlogPredmeta { get; set; }
    }

    public class DokumentacijaViewModel
    {
        public bool CV { get; set; }
        public bool MotivacionoPismo { get; set; }
    }

    public class PrijedlogPredmetaViewModel
    {
        public List<PredmetRow> Rows { get; set; } = new List<PredmetRow>();
    }

    public class PredmetRow
    {
        public string PredmetHome { get; set; }
        public string PredmetAccepting { get; set; }
    }
}
