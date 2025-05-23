using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemErasmusRazmjena.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateNotifikacijaKorisnikRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ID",
                table: "Notifikacije",
                newName: "Id");

            migrationBuilder.AddColumn<DateTime>(
                name: "Datum",
                table: "Notifikacije",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Datum",
                table: "Notifikacije");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Notifikacije",
                newName: "ID");
        }
    }
}
