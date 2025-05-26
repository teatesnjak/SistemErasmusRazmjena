using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemErasmusRazmjena.Data.Migrations
{
    /// <inheritdoc />
    public partial class MigracijaZaDodavanjePrijavaIdUPredmet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PrijavaId",
                table: "Predmeti",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StudentID",
                table: "Predmeti",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Predmeti_PrijavaId",
                table: "Predmeti",
                column: "PrijavaId");

            migrationBuilder.CreateIndex(
                name: "IX_Predmeti_StudentID",
                table: "Predmeti",
                column: "StudentID");

            migrationBuilder.AddForeignKey(
                name: "FK_Predmeti_AspNetUsers_StudentID",
                table: "Predmeti",
                column: "StudentID",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Predmeti_Prijave_PrijavaId",
                table: "Predmeti",
                column: "PrijavaId",
                principalTable: "Prijave",
                principalColumn: "ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Predmeti_AspNetUsers_StudentID",
                table: "Predmeti");

            migrationBuilder.DropForeignKey(
                name: "FK_Predmeti_Prijave_PrijavaId",
                table: "Predmeti");

            migrationBuilder.DropIndex(
                name: "IX_Predmeti_PrijavaId",
                table: "Predmeti");

            migrationBuilder.DropIndex(
                name: "IX_Predmeti_StudentID",
                table: "Predmeti");

            migrationBuilder.DropColumn(
                name: "PrijavaId",
                table: "Predmeti");

            migrationBuilder.DropColumn(
                name: "StudentID",
                table: "Predmeti");
        }
    }
}
