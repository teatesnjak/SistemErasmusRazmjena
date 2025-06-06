using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemErasmusRazmjena.Data.Migrations
{
    /// <inheritdoc />
    public partial class ZadnjeIspravke : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Prijave_ErasmusProgrami_ErasmusProgramID",
                table: "Prijave");

            migrationBuilder.AddForeignKey(
                name: "FK_Prijave_ErasmusProgrami_ErasmusProgramID",
                table: "Prijave",
                column: "ErasmusProgramID",
                principalTable: "ErasmusProgrami",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Prijave_ErasmusProgrami_ErasmusProgramID",
                table: "Prijave");

            migrationBuilder.AddForeignKey(
                name: "FK_Prijave_ErasmusProgrami_ErasmusProgramID",
                table: "Prijave",
                column: "ErasmusProgramID",
                principalTable: "ErasmusProgrami",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
