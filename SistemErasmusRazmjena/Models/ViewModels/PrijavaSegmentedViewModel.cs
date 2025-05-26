using SistemErasmusRazmjena.Models;

public class PrijavaSegmentedViewModel
{
    public List<Prijava> InProgress { get; set; }
    public List<Prijava> Successful { get; set; }
    public List<Prijava> Unsuccessful { get; set; }
}
