using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartGridAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddUserIdToTeamMember : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "MaintenanceTeamMembers",
                type: "int",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "MaintenanceTeamMembers",
                keyColumn: "MemberId",
                keyValue: 1,
                column: "UserId",
                value: null);

            migrationBuilder.UpdateData(
                table: "MaintenanceTeamMembers",
                keyColumn: "MemberId",
                keyValue: 2,
                column: "UserId",
                value: null);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserId",
                table: "MaintenanceTeamMembers");
        }
    }
}
