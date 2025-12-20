using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedicationAssist.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWebLoginToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WebLoginTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsUsed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    UsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebLoginTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WebLoginTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WebLoginTokens_ExpiresAt",
                table: "WebLoginTokens",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_WebLoginTokens_Token",
                table: "WebLoginTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WebLoginTokens_UserId",
                table: "WebLoginTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_WebLoginTokens_UserId_IsUsed_ExpiresAt",
                table: "WebLoginTokens",
                columns: new[] { "UserId", "IsUsed", "ExpiresAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WebLoginTokens");
        }
    }
}
