using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartGridAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddAcceptedByField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AcceptedBy",
                table: "MaintenanceTickets",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcceptedBy",
                table: "MaintenanceTickets");
        }
    }
}
