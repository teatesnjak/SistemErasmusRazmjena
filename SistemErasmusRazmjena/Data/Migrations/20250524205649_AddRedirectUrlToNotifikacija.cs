using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemErasmusRazmjena.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRedirectUrlToNotifikacija : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RedirectUrl",
                table: "Notifikacije",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RedirectUrl",
                table: "Notifikacije");
        }
    }
}
