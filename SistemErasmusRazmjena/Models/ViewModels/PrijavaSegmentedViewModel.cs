using SistemErasmusRazmjena.Models;

public class PrijavaSegmentedViewModel
{
    public List<Prijava> UTOKU { get; set; } = new List<Prijava>();
    public List<Prijava> USPJESNA { get; set; } = new List<Prijava>();
    public List<Prijava> NEUSPJESNA { get; set; } = new List<Prijava>();
}
