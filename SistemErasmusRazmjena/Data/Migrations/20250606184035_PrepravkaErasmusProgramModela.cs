using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemErasmusRazmjena.Data.Migrations
{
    /// <inheritdoc />
    public partial class PrepravkaErasmusProgramModela : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PrijedloziPredmeta_ErasmusProgrami_ErasmusProgramID",
                table: "PrijedloziPredmeta");

            migrationBuilder.AddForeignKey(
                name: "FK_PrijedloziPredmeta_ErasmusProgrami_ErasmusProgramID",
                table: "PrijedloziPredmeta",
                column: "ErasmusProgramID",
                principalTable: "ErasmusProgrami",
                principalColumn: "ID",
                onDelete: ReferentialAction.SetNull); // Change to SET NULL
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PrijedloziPredmeta_ErasmusProgrami_ErasmusProgramID",
                table: "PrijedloziPredmeta");

            migrationBuilder.AddForeignKey(
                name: "FK_PrijedloziPredmeta_ErasmusProgrami_ErasmusProgramID",
                table: "PrijedloziPredmeta",
                column: "ErasmusProgramID",
                principalTable: "ErasmusProgrami",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade); // Revert to CASCADE
        }
    }
}
