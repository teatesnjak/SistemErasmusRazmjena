using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SistemErasmusRazmjena.Models;

namespace SistemErasmusRazmjena.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<ErasmusProgram> ErasmusProgrami { get; set; }
        public DbSet<Prijava> Prijave { get; set; }
        public DbSet<Dokumentacija> Dokumentacije { get; set; }
        public DbSet<PrijedlogPredmeta> PrijedloziPredmeta { get; set; }
        public DbSet<Predmet> Predmeti { get; set; }
        public DbSet<Notifikacija> Notifikacije { get; set; }
        public DbSet<Fakultet> Fakulteti { get; set; }
        public DbSet<Faculty> Faculties { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Prijava - Student (ApplicationUser)
            modelBuilder.Entity<Prijava>()
                .HasOne(p => p.Student)
                .WithMany()
                .HasForeignKey(p => p.StudentID)
                .OnDelete(DeleteBehavior.Restrict); // da se ne briše Student ako se briše Prijava

            // Prijava - ErasmusProgram
            modelBuilder.Entity<Prijava>()
                .HasOne(p => p.ErasmusProgram)
                .WithMany()
                .HasForeignKey(p => p.ErasmusProgramID)
                .OnDelete(DeleteBehavior.Restrict);

            // Prijava - Dokumentacija
            modelBuilder.Entity<Prijava>()
                .HasOne(p => p.Dokumentacija)
                .WithOne()
                .HasForeignKey<Prijava>(p => p.DokumentacijaID)
                .OnDelete(DeleteBehavior.Cascade);

            // Prijava - PrijedlogPredmeta
            modelBuilder.Entity<Prijava>()
                .HasOne(p => p.PrijedlogPredmeta)
                .WithOne()
                .HasForeignKey<Prijava>(p => p.PrijedlogPredmetaID)
                .OnDelete(DeleteBehavior.Cascade);

            // PrijedlogPredmeta - Predmeti
            modelBuilder.Entity<Predmet>()
                .HasOne<PrijedlogPredmeta>()
                .WithMany(p => p.Rows)
                .HasForeignKey(p => p.PrijedlogPredmetaID)
                .OnDelete(DeleteBehavior.Cascade);



            modelBuilder.Entity<Notifikacija>()
                .HasOne(n => n.Korisnik)
                .WithMany()
                .HasForeignKey(n => n.KorisnikID)
                .HasPrincipalKey(u => u.Id);

            // Add a unique constraint to prevent duplicate applications
            modelBuilder.Entity<Prijava>()
                .HasIndex(p => new { p.StudentID, p.ErasmusProgramID })
                .IsUnique();
        }

    }

    public class Faculty
    {
        public int Id { get; set; }
        public string Naziv { get; set; }
    }
}
