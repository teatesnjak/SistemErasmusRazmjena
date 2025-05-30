using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemErasmusRazmjena.Data.Migrations
{
    /// <inheritdoc />
    public partial class MigracijaZaDodavanjeRedovaZaPredmete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Name",
                table: "ErasmusProgrami");

            migrationBuilder.AddColumn<int>(
                name: "ErasmusProgramID",
                table: "PrijedloziPredmeta",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PrijedloziPredmeta_ErasmusProgramID",
                table: "PrijedloziPredmeta",
                column: "ErasmusProgramID");

            migrationBuilder.AddForeignKey(
                name: "FK_PrijedloziPredmeta_ErasmusProgrami_ErasmusProgramID",
                table: "PrijedloziPredmeta",
                column: "ErasmusProgramID",
                principalTable: "ErasmusProgrami",
                principalColumn: "ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PrijedloziPredmeta_ErasmusProgrami_ErasmusProgramID",
                table: "PrijedloziPredmeta");

            migrationBuilder.DropIndex(
                name: "IX_PrijedloziPredmeta_ErasmusProgramID",
                table: "PrijedloziPredmeta");

            migrationBuilder.DropColumn(
                name: "ErasmusProgramID",
                table: "PrijedloziPredmeta");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "ErasmusProgrami",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
