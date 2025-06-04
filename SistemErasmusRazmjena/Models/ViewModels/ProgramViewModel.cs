using SistemErasmusRazmjena.Models;

namespace SistemErasmusRazmjena.Models.ViewModels
{
    public class ProgramViewModel
    {
        public ErasmusProgram Program { get; set; } = null!;
        public bool HasApplied { get; set; }
    }
}
