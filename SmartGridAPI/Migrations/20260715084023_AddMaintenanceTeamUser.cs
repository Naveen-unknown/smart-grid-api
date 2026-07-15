using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartGridAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddMaintenanceTeamUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "EnergyReadings",
                keyColumn: "Id",
                keyValue: 101,
                column: "Timestamp",
                value: new DateTime(2026, 7, 15, 7, 40, 22, 484, DateTimeKind.Utc).AddTicks(4027));

            migrationBuilder.UpdateData(
                table: "EnergyReadings",
                keyColumn: "Id",
                keyValue: 102,
                column: "Timestamp",
                value: new DateTime(2026, 7, 15, 6, 40, 22, 484, DateTimeKind.Utc).AddTicks(4297));

            migrationBuilder.UpdateData(
                table: "EnergyReadings",
                keyColumn: "Id",
                keyValue: 103,
                column: "Timestamp",
                value: new DateTime(2026, 7, 15, 5, 40, 22, 484, DateTimeKind.Utc).AddTicks(4301));

            migrationBuilder.UpdateData(
                table: "EnergyReadings",
                keyColumn: "Id",
                keyValue: 104,
                column: "Timestamp",
                value: new DateTime(2026, 7, 14, 8, 40, 22, 484, DateTimeKind.Utc).AddTicks(4305));

            migrationBuilder.UpdateData(
                table: "EnergyReadings",
                keyColumn: "Id",
                keyValue: 105,
                column: "Timestamp",
                value: new DateTime(2026, 7, 14, 8, 40, 22, 484, DateTimeKind.Utc).AddTicks(4309));

            migrationBuilder.UpdateData(
                table: "EnergyReadings",
                keyColumn: "Id",
                keyValue: 106,
                column: "Timestamp",
                value: new DateTime(2026, 7, 13, 8, 40, 22, 484, DateTimeKind.Utc).AddTicks(4367));

            migrationBuilder.UpdateData(
                table: "EnergyReadings",
                keyColumn: "Id",
                keyValue: 107,
                column: "Timestamp",
                value: new DateTime(2026, 7, 12, 8, 40, 22, 484, DateTimeKind.Utc).AddTicks(4372));

            migrationBuilder.UpdateData(
                table: "Faults",
                keyColumn: "Id",
                keyValue: 101,
                column: "ReportedAt",
                value: new DateTime(2026, 7, 15, 6, 40, 22, 483, DateTimeKind.Utc).AddTicks(7469));

            migrationBuilder.UpdateData(
                table: "Faults",
                keyColumn: "Id",
                keyValue: 102,
                column: "ReportedAt",
                value: new DateTime(2026, 7, 15, 3, 40, 22, 483, DateTimeKind.Utc).AddTicks(8108));

            migrationBuilder.UpdateData(
                table: "Faults",
                keyColumn: "Id",
                keyValue: 103,
                column: "ReportedAt",
                value: new DateTime(2026, 7, 15, 7, 40, 22, 483, DateTimeKind.Utc).AddTicks(8115));

            migrationBuilder.UpdateData(
                table: "Faults",
                keyColumn: "Id",
                keyValue: 104,
                columns: new[] { "ReportedAt", "ResolvedAt" },
                values: new object[] { new DateTime(2026, 7, 14, 8, 40, 22, 483, DateTimeKind.Utc).AddTicks(8118), new DateTime(2026, 7, 14, 20, 40, 22, 483, DateTimeKind.Utc).AddTicks(8135) });

            migrationBuilder.UpdateData(
                table: "Outages",
                keyColumn: "Id",
                keyValue: 101,
                column: "StartedAt",
                value: new DateTime(2026, 7, 15, 4, 40, 22, 484, DateTimeKind.Utc).AddTicks(152));

            migrationBuilder.UpdateData(
                table: "Outages",
                keyColumn: "Id",
                keyValue: 102,
                columns: new[] { "RestoredAt", "StartedAt" },
                values: new object[] { new DateTime(2026, 7, 14, 22, 40, 22, 484, DateTimeKind.Utc).AddTicks(855), new DateTime(2026, 7, 14, 8, 40, 22, 484, DateTimeKind.Utc).AddTicks(853) });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Email", "IsActive", "LastLoginAt", "PasswordHash", "Role", "Username" },
                values: new object[] { 3, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "team@smartgrid.com", true, null, "$2a$11$t3KQzl4lh40owPIXHkxXCOJpfYSMIvH/YF6CygLub4220I5GMGXnm", "Maintenance Team", "maintenance_team" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.UpdateData(
                table: "EnergyReadings",
                keyColumn: "Id",
                keyValue: 101,
                column: "Timestamp",
                value: new DateTime(2026, 7, 15, 5, 37, 51, 861, DateTimeKind.Utc).AddTicks(3417));

            migrationBuilder.UpdateData(
                table: "EnergyReadings",
                keyColumn: "Id",
                keyValue: 102,
                column: "Timestamp",
                value: new DateTime(2026, 7, 15, 4, 37, 51, 861, DateTimeKind.Utc).AddTicks(3727));

            migrationBuilder.UpdateData(
                table: "EnergyReadings",
                keyColumn: "Id",
                keyValue: 103,
                column: "Timestamp",
                value: new DateTime(2026, 7, 15, 3, 37, 51, 861, DateTimeKind.Utc).AddTicks(3729));

            migrationBuilder.UpdateData(
                table: "EnergyReadings",
                keyColumn: "Id",
                keyValue: 104,
                column: "Timestamp",
                value: new DateTime(2026, 7, 14, 6, 37, 51, 861, DateTimeKind.Utc).AddTicks(3732));

            migrationBuilder.UpdateData(
                table: "EnergyReadings",
                keyColumn: "Id",
                keyValue: 105,
                column: "Timestamp",
                value: new DateTime(2026, 7, 14, 6, 37, 51, 861, DateTimeKind.Utc).AddTicks(3735));

            migrationBuilder.UpdateData(
                table: "EnergyReadings",
                keyColumn: "Id",
                keyValue: 106,
                column: "Timestamp",
                value: new DateTime(2026, 7, 13, 6, 37, 51, 861, DateTimeKind.Utc).AddTicks(3751));

            migrationBuilder.UpdateData(
                table: "EnergyReadings",
                keyColumn: "Id",
                keyValue: 107,
                column: "Timestamp",
                value: new DateTime(2026, 7, 12, 6, 37, 51, 861, DateTimeKind.Utc).AddTicks(3754));

            migrationBuilder.UpdateData(
                table: "Faults",
                keyColumn: "Id",
                keyValue: 101,
                column: "ReportedAt",
                value: new DateTime(2026, 7, 15, 4, 37, 51, 860, DateTimeKind.Utc).AddTicks(9043));

            migrationBuilder.UpdateData(
                table: "Faults",
                keyColumn: "Id",
                keyValue: 102,
                column: "ReportedAt",
                value: new DateTime(2026, 7, 15, 1, 37, 51, 860, DateTimeKind.Utc).AddTicks(9498));

            migrationBuilder.UpdateData(
                table: "Faults",
                keyColumn: "Id",
                keyValue: 103,
                column: "ReportedAt",
                value: new DateTime(2026, 7, 15, 5, 37, 51, 860, DateTimeKind.Utc).AddTicks(9503));

            migrationBuilder.UpdateData(
                table: "Faults",
                keyColumn: "Id",
                keyValue: 104,
                columns: new[] { "ReportedAt", "ResolvedAt" },
                values: new object[] { new DateTime(2026, 7, 14, 6, 37, 51, 860, DateTimeKind.Utc).AddTicks(9505), new DateTime(2026, 7, 14, 18, 37, 51, 860, DateTimeKind.Utc).AddTicks(9515) });

            migrationBuilder.UpdateData(
                table: "Outages",
                keyColumn: "Id",
                keyValue: 101,
                column: "StartedAt",
                value: new DateTime(2026, 7, 15, 2, 37, 51, 861, DateTimeKind.Utc).AddTicks(954));

            migrationBuilder.UpdateData(
                table: "Outages",
                keyColumn: "Id",
                keyValue: 102,
                columns: new[] { "RestoredAt", "StartedAt" },
                values: new object[] { new DateTime(2026, 7, 14, 20, 37, 51, 861, DateTimeKind.Utc).AddTicks(1260), new DateTime(2026, 7, 14, 6, 37, 51, 861, DateTimeKind.Utc).AddTicks(1258) });
        }
    }
}
