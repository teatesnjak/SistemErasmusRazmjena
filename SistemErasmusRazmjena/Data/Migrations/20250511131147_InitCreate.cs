using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemErasmusRazmjena.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FakultetID",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Uloga",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Dokumentacije",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PrijavaID = table.Column<int>(type: "int", nullable: false),
                    CV = table.Column<bool>(type: "bit", nullable: false),
                    MotivacionoPismo = table.Column<bool>(type: "bit", nullable: false),
                    UgovorOUcenju = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dokumentacije", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "ErasmusProgrami",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Semestar = table.Column<int>(type: "int", nullable: false),
                    AkademskaGodina = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Univerzitet = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Opis = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErasmusProgrami", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Notifikacije",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KorisnikID = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Vrijeme = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Sadrzaj = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Procitano = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifikacije", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Notifikacije_AspNetUsers_KorisnikID",
                        column: x => x.KorisnikID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PrijedloziPredmeta",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PrijavaID = table.Column<int>(type: "int", nullable: false),
                    VrijemeIzmjene = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrijedloziPredmeta", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Predmeti",
                columns: table => new
                {
                    PredmetID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PrijedlogPredmetaID = table.Column<int>(type: "int", nullable: false),
                    PredmetHome = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PredmetAccepting = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Predmeti", x => x.PredmetID);
                    table.ForeignKey(
                        name: "FK_Predmeti_PrijedloziPredmeta_PrijedlogPredmetaID",
                        column: x => x.PrijedlogPredmetaID,
                        principalTable: "PrijedloziPredmeta",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Prijave",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentID = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ErasmusProgramID = table.Column<int>(type: "int", nullable: false),
                    DokumentacijaID = table.Column<int>(type: "int", nullable: false),
                    PrijedlogPredmetaID = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Prijave", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Prijave_AspNetUsers_StudentID",
                        column: x => x.StudentID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Prijave_Dokumentacije_DokumentacijaID",
                        column: x => x.DokumentacijaID,
                        principalTable: "Dokumentacije",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Prijave_ErasmusProgrami_ErasmusProgramID",
                        column: x => x.ErasmusProgramID,
                        principalTable: "ErasmusProgrami",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Prijave_PrijedloziPredmeta_PrijedlogPredmetaID",
                        column: x => x.PrijedlogPredmetaID,
                        principalTable: "PrijedloziPredmeta",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Notifikacije_KorisnikID",
                table: "Notifikacije",
                column: "KorisnikID");

            migrationBuilder.CreateIndex(
                name: "IX_Predmeti_PrijedlogPredmetaID",
                table: "Predmeti",
                column: "PrijedlogPredmetaID");

            migrationBuilder.CreateIndex(
                name: "IX_Prijave_DokumentacijaID",
                table: "Prijave",
                column: "DokumentacijaID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Prijave_ErasmusProgramID",
                table: "Prijave",
                column: "ErasmusProgramID");

            migrationBuilder.CreateIndex(
                name: "IX_Prijave_PrijedlogPredmetaID",
                table: "Prijave",
                column: "PrijedlogPredmetaID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Prijave_StudentID",
                table: "Prijave",
                column: "StudentID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Notifikacije");

            migrationBuilder.DropTable(
                name: "Predmeti");

            migrationBuilder.DropTable(
                name: "Prijave");

            migrationBuilder.DropTable(
                name: "Dokumentacije");

            migrationBuilder.DropTable(
                name: "ErasmusProgrami");

            migrationBuilder.DropTable(
                name: "PrijedloziPredmeta");

            migrationBuilder.DropColumn(
                name: "FakultetID",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Uloga",
                table: "AspNetUsers");
        }
    }
}
