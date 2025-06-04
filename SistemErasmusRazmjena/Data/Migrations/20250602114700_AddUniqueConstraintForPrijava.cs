using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemErasmusRazmjena.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueConstraintForPrijava : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Prijave_StudentID",
                table: "Prijave");

            migrationBuilder.CreateIndex(
                name: "IX_Prijave_StudentID_ErasmusProgramID",
                table: "Prijave",
                columns: new[] { "StudentID", "ErasmusProgramID" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Prijave_StudentID_ErasmusProgramID",
                table: "Prijave");

            migrationBuilder.CreateIndex(
                name: "IX_Prijave_StudentID",
                table: "Prijave",
                column: "StudentID");
        }
    }
}
