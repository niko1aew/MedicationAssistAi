using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedicationAssist.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTelegramLoginTokensTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TelegramLoginTokens",
                columns: table => new
                {
                    Token = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsAuthorized = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AuthorizedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelegramLoginTokens", x => x.Token);
                    table.ForeignKey(
                        name: "FK_TelegramLoginTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TelegramLoginTokens_ExpiresAt",
                table: "TelegramLoginTokens",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_TelegramLoginTokens_UserId",
                table: "TelegramLoginTokens",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TelegramLoginTokens");
        }
    }
}
