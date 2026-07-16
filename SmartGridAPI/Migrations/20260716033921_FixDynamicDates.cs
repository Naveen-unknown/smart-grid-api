using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartGridAPI.Migrations
{
    /// <inheritdoc />
    public partial class FixDynamicDates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "EnergyReadings",
                keyColumn: "Id",
                keyValue: 101,
                column: "Timestamp",
                value: new DateTime(2026, 7, 1, 15, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "EnergyReadings",
                keyColumn: "Id",
                keyValue: 102,
                column: "Timestamp",
                value: new DateTime(2026, 7, 1, 14, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "EnergyReadings",
                keyColumn: "Id",
                keyValue: 103,
                column: "Timestamp",
                value: new DateTime(2026, 7, 1, 13, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "EnergyReadings",
                keyColumn: "Id",
                keyValue: 104,
                column: "Timestamp",
                value: new DateTime(2026, 6, 30, 15, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "EnergyReadings",
                keyColumn: "Id",
                keyValue: 105,
                column: "Timestamp",
                value: new DateTime(2026, 6, 30, 15, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "EnergyReadings",
                keyColumn: "Id",
                keyValue: 106,
                column: "Timestamp",
                value: new DateTime(2026, 6, 29, 15, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "EnergyReadings",
                keyColumn: "Id",
                keyValue: 107,
                column: "Timestamp",
                value: new DateTime(2026, 6, 28, 15, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "Faults",
                keyColumn: "Id",
                keyValue: 101,
                column: "ReportedAt",
                value: new DateTime(2026, 7, 1, 10, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "Faults",
                keyColumn: "Id",
                keyValue: 102,
                column: "ReportedAt",
                value: new DateTime(2026, 7, 1, 8, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "Faults",
                keyColumn: "Id",
                keyValue: 103,
                column: "ReportedAt",
                value: new DateTime(2026, 7, 1, 12, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "Faults",
                keyColumn: "Id",
                keyValue: 104,
                columns: new[] { "ReportedAt", "ResolvedAt" },
                values: new object[] { new DateTime(2026, 6, 30, 10, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 6, 30, 20, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.UpdateData(
                table: "Outages",
                keyColumn: "Id",
                keyValue: 101,
                column: "StartedAt",
                value: new DateTime(2026, 7, 1, 14, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "Outages",
                keyColumn: "Id",
                keyValue: 102,
                columns: new[] { "RestoredAt", "StartedAt" },
                values: new object[] { new DateTime(2026, 6, 30, 18, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 6, 30, 10, 0, 0, 0, DateTimeKind.Utc) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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
        }
    }
}
