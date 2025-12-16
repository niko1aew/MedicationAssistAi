using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedicationAssist.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTelegramFieldsToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "TelegramUserId",
                table: "Users",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TelegramUsername",
                table: "Users",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_TelegramUserId",
                table: "Users",
                column: "TelegramUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_TelegramUserId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TelegramUserId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TelegramUsername",
                table: "Users");
        }
    }
}
