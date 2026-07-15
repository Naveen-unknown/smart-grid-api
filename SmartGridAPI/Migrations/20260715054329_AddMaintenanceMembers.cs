using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SmartGridAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddMaintenanceMembers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProofPhotoUrl",
                table: "MaintenanceTickets",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MaintenanceTeamMembers",
                columns: table => new
                {
                    MemberId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Role = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PhoneNumber = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TeamId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceTeamMembers", x => x.MemberId);
                    table.ForeignKey(
                        name: "FK_MaintenanceTeamMembers_MaintenanceTeams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "MaintenanceTeams",
                        principalColumn: "TeamId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "MaintenanceTeamMembers",
                columns: new[] { "MemberId", "Name", "PhoneNumber", "Role", "TeamId" },
                values: new object[,]
                {
                    { 1, "Lead Engineer", "+919344255537", "Senior Engineer", 1 },
                    { 2, "Technician John", "+919876543211", "Technician", 2 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceTeamMembers_TeamId",
                table: "MaintenanceTeamMembers",
                column: "TeamId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MaintenanceTeamMembers");

            migrationBuilder.DropColumn(
                name: "ProofPhotoUrl",
                table: "MaintenanceTickets");
        }
    }
}
