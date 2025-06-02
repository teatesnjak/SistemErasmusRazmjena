using System.Collections.Generic;
using SistemErasmusRazmjena.Models;

namespace SistemErasmusRazmjena.Models.ViewModels
{
    public class SearchViewModel
    {
        public string Query { get; set; } = string.Empty;
        public List<ErasmusProgram> Programs { get; set; } = new List<ErasmusProgram>();
        public PrijavaSegmentedViewModel Applications { get; set; } = new PrijavaSegmentedViewModel();
        public int? SelectedSemester { get; set; }
        public string SelectedStatus { get; set; }
    }
}
