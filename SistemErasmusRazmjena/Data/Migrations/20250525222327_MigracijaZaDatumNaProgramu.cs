using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemErasmusRazmjena.Data.Migrations
{
    /// <inheritdoc />
    public partial class MigracijaZaDatumNaProgramu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DateAdded",
                table: "ErasmusProgrami",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "ErasmusProgrami",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            // Check if the 'Faculties' table exists before creating it
            if (!TableExists(migrationBuilder, "Faculties"))
            {
                migrationBuilder.CreateTable(
                    name: "Faculties",
                    columns: table => new
                    {
                        Id = table.Column<int>(type: "int", nullable: false)
                            .Annotation("SqlServer:Identity", "1, 1"),
                        Naziv = table.Column<string>(type: "nvarchar(max)", nullable: false)
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_Faculties", x => x.Id);
                    });
            }
        }

        private bool TableExists(MigrationBuilder migrationBuilder, string tableName)
        {
            var sql = $"SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{tableName}'";
            var result = migrationBuilder.Sql(sql);
            return result != null;
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Faculties");

            migrationBuilder.DropColumn(
                name: "DateAdded",
                table: "ErasmusProgrami");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "ErasmusProgrami");
        }
    }
}
