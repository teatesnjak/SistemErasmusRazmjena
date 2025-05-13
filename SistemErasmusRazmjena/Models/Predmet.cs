namespace SistemErasmusRazmjena.Models
{
    public enum StatusPredmeta { NACEKANJU, ODOBRENO, ODBIJENO }

    public class Predmet
    {
        public int PredmetID { get; set; }
        public int PrijedlogPredmetaID { get; set; }
        public string PredmetHome { get; set; }
        public string PredmetAccepting { get; set; }
        public StatusPredmeta Status { get; set; } = StatusPredmeta.NACEKANJU;
    }

}
