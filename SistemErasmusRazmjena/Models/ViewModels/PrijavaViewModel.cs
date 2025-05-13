namespace SistemErasmusRazmjena.Models.ViewModels
{
    public class PrijavaViewModel
    {
        public int ErasmusProgramID { get; set; }
        public List<PredmetViewModel> Predmeti { get; set; }
    }

    public class PredmetViewModel
    {
        public string PredmetHome { get; set; }
        public string PredmetAccepting { get; set; }
    }
}
