using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedicationAssist.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SetDefaultTimeZone : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "TimeZoneId",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "Europe/Moscow",
                oldClrType: typeof(string),
                oldType: "text");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "TimeZoneId",
                table: "Users",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldDefaultValue: "Europe/Moscow");
        }
    }
}
