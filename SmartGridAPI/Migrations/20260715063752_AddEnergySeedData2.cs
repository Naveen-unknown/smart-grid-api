using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SmartGridAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddEnergySeedData2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "EnergyReadings",
                columns: new[] { "Id", "Consumption", "Current", "Frequency", "MeterId", "NodeId", "PowerFactor", "Production", "Timestamp", "UserId", "Voltage" },
                values: new object[,]
                {
                    { 101, 120m, 15m, 50m, null, 1, 0.95m, 0m, new DateTime(2026, 7, 15, 5, 37, 51, 861, DateTimeKind.Utc).AddTicks(3417), 1, 220m },
                    { 102, 150m, 20m, 50m, null, 2, 0.92m, 50m, new DateTime(2026, 7, 15, 4, 37, 51, 861, DateTimeKind.Utc).AddTicks(3727), 1, 215m },
                    { 103, 80m, 10m, 50m, null, 3, 0.98m, 0m, new DateTime(2026, 7, 15, 3, 37, 51, 861, DateTimeKind.Utc).AddTicks(3729), 1, 230m },
                    { 104, 125m, 16m, 50m, null, 1, 0.94m, 0m, new DateTime(2026, 7, 14, 6, 37, 51, 861, DateTimeKind.Utc).AddTicks(3732), 1, 225m },
                    { 105, 160m, 22m, 50m, null, 2, 0.90m, 40m, new DateTime(2026, 7, 14, 6, 37, 51, 861, DateTimeKind.Utc).AddTicks(3735), 1, 210m },
                    { 106, 75m, 9m, 50m, null, 3, 0.99m, 0m, new DateTime(2026, 7, 13, 6, 37, 51, 861, DateTimeKind.Utc).AddTicks(3751), 1, 235m },
                    { 107, 135m, 18m, 50m, null, 1, 0.93m, 0m, new DateTime(2026, 7, 12, 6, 37, 51, 861, DateTimeKind.Utc).AddTicks(3754), 1, 218m }
                });

            migrationBuilder.InsertData(
                table: "Faults",
                columns: new[] { "Id", "AIPrediction", "AIRecommendation", "AcknowledgedAt", "AssignedTo", "ConfidenceScore", "Description", "FaultType", "NodeId", "ReportedAt", "ReportedByUserId", "ResolutionNotes", "ResolvedAt", "Severity", "Status" },
                values: new object[,]
                {
                    { 101, null, null, null, null, null, "Transformer Oil Leak", "", 1, new DateTime(2026, 7, 15, 4, 37, 51, 860, DateTimeKind.Utc).AddTicks(9043), 1, null, null, "Critical", "Reported" },
                    { 102, null, null, null, null, null, "Voltage Sag", "", 2, new DateTime(2026, 7, 15, 1, 37, 51, 860, DateTimeKind.Utc).AddTicks(9498), 1, null, null, "Medium", "InProgress" },
                    { 103, null, null, null, null, null, "Phase Imbalance", "", 3, new DateTime(2026, 7, 15, 5, 37, 51, 860, DateTimeKind.Utc).AddTicks(9503), 1, null, null, "High", "Reported" },
                    { 104, null, null, null, null, null, "Broken Insulator", "", 4, new DateTime(2026, 7, 14, 6, 37, 51, 860, DateTimeKind.Utc).AddTicks(9505), 1, null, new DateTime(2026, 7, 14, 18, 37, 51, 860, DateTimeKind.Utc).AddTicks(9515), "Low", "Resolved" }
                });

            migrationBuilder.InsertData(
                table: "Outages",
                columns: new[] { "Id", "AIAnalysis", "ActionTaken", "AffectedArea", "AffectedCustomers", "Cause", "EstimatedRestorationTime", "NodeId", "OutageType", "ReportedByUserId", "RestoredAt", "StartedAt", "Status" },
                values: new object[,]
                {
                    { 101, null, null, "", 450, null, null, 2, "Unplanned", null, null, new DateTime(2026, 7, 15, 2, 37, 51, 861, DateTimeKind.Utc).AddTicks(954), "Ongoing" },
                    { 102, null, null, "", 1200, null, null, 5, "Unplanned", null, new DateTime(2026, 7, 14, 20, 37, 51, 861, DateTimeKind.Utc).AddTicks(1260), new DateTime(2026, 7, 14, 6, 37, 51, 861, DateTimeKind.Utc).AddTicks(1258), "Restored" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "EnergyReadings",
                keyColumn: "Id",
                keyValue: 101);

            migrationBuilder.DeleteData(
                table: "EnergyReadings",
                keyColumn: "Id",
                keyValue: 102);

            migrationBuilder.DeleteData(
                table: "EnergyReadings",
                keyColumn: "Id",
                keyValue: 103);

            migrationBuilder.DeleteData(
                table: "EnergyReadings",
                keyColumn: "Id",
                keyValue: 104);

            migrationBuilder.DeleteData(
                table: "EnergyReadings",
                keyColumn: "Id",
                keyValue: 105);

            migrationBuilder.DeleteData(
                table: "EnergyReadings",
                keyColumn: "Id",
                keyValue: 106);

            migrationBuilder.DeleteData(
                table: "EnergyReadings",
                keyColumn: "Id",
                keyValue: 107);

            migrationBuilder.DeleteData(
                table: "Faults",
                keyColumn: "Id",
                keyValue: 101);

            migrationBuilder.DeleteData(
                table: "Faults",
                keyColumn: "Id",
                keyValue: 102);

            migrationBuilder.DeleteData(
                table: "Faults",
                keyColumn: "Id",
                keyValue: 103);

            migrationBuilder.DeleteData(
                table: "Faults",
                keyColumn: "Id",
                keyValue: 104);

            migrationBuilder.DeleteData(
                table: "Outages",
                keyColumn: "Id",
                keyValue: 101);

            migrationBuilder.DeleteData(
                table: "Outages",
                keyColumn: "Id",
                keyValue: 102);
        }
    }
}
