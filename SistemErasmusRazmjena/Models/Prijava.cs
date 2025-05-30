using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemErasmusRazmjena.Models
{
    public enum StatusPrijave 
    { 
        UTOKU = 0, 
        USPJESNA = 1, 
        NEUSPJESNA = 2
    }

    public class Prijava
    {
        public int ID { get; set; }

        [ForeignKey("Student")]
        public string StudentID { get; set; }
        public virtual ApplicationUser Student { get; set; }

        public int ErasmusProgramID { get; set; }
        public virtual ErasmusProgram ErasmusProgram { get; set; }

        public int DokumentacijaID { get; set; }
        public virtual Dokumentacija Dokumentacija { get; set; }

        public int PrijedlogPredmetaID { get; set; }
        public virtual PrijedlogPredmeta PrijedlogPredmeta { get; set; }

        public StatusPrijave Status { get; set; } = StatusPrijave.UTOKU;

        public DateTime DateCreated { get; set; } = DateTime.Now;

    }
}