namespace SistemErasmusRazmjena.Models
{
    public class PrijedlogPredmeta
    {
        public int ID { get; set; }
        public int PrijavaID { get; set; }
        public DateTime VrijemeIzmjene { get; set; } = DateTime.Now;

        public virtual List<Predmet> Rows { get; set; }

    }
}
