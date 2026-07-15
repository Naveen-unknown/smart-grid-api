using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartGridAPI.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSeededPasswords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$t3KQzl4lh40owPIXHkxXCOJpfYSMIvH/YF6CygLub4220I5GMGXnm");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                column: "PasswordHash",
                value: "$2a$11$t3KQzl4lh40owPIXHkxXCOJpfYSMIvH/YF6CygLub4220I5GMGXnm");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$LfN0evJWh8o5cRxuELfV8u5C2sAYMwkMxb3PpT1qIv9xt8Ol/iqDq");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                column: "PasswordHash",
                value: "$2a$11$LfN0evJWh8o5cRxuELfV8u5C2sAYMwkMxb3PpT1qIv9xt8Ol/iqDq");
        }
    }
}
