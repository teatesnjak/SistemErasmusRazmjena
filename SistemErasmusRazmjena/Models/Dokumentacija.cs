namespace SistemErasmusRazmjena.Models
{
    public class Dokumentacija
    {
        public int ID { get; set; } // This is the primary key
        public int PrijavaID { get; set; }
        public bool CV { get; set; }
        public bool MotivacionoPismo { get; set; }
        public bool UgovorOUcenju { get; set; }

        // Add this property to resolve the error
        public int DokumentacijaID => ID; // Alias for ID
    }
}
