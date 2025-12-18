using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedicationAssist.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameLinkTokensTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "link_tokens",
                newName: "LinkTokens");

            migrationBuilder.RenameIndex(
                name: "IX_link_tokens_Token",
                table: "LinkTokens",
                newName: "IX_LinkTokens_Token");

            migrationBuilder.RenameIndex(
                name: "IX_link_tokens_UserId",
                table: "LinkTokens",
                newName: "IX_LinkTokens_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_link_tokens_ExpiresAt",
                table: "LinkTokens",
                newName: "IX_LinkTokens_ExpiresAt");

            migrationBuilder.RenameIndex(
                name: "IX_link_tokens_UserId_IsUsed_ExpiresAt",
                table: "LinkTokens",
                newName: "IX_LinkTokens_UserId_IsUsed_ExpiresAt");

            // Rename constraints to match PascalCase naming
            migrationBuilder.Sql(@"
                ALTER TABLE ""LinkTokens"" RENAME CONSTRAINT ""PK_link_tokens"" TO ""PK_LinkTokens"";
                ALTER TABLE ""LinkTokens"" RENAME CONSTRAINT ""FK_link_tokens_Users_UserId"" TO ""FK_LinkTokens_Users_UserId"";
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Rename constraints back to snake_case
            migrationBuilder.Sql(@"
                ALTER TABLE ""LinkTokens"" RENAME CONSTRAINT ""PK_LinkTokens"" TO ""PK_link_tokens"";
                ALTER TABLE ""LinkTokens"" RENAME CONSTRAINT ""FK_LinkTokens_Users_UserId"" TO ""FK_link_tokens_Users_UserId"";
            ");

            migrationBuilder.RenameTable(
                name: "LinkTokens",
                newName: "link_tokens");

            migrationBuilder.RenameIndex(
                name: "IX_LinkTokens_Token",
                table: "link_tokens",
                newName: "IX_link_tokens_Token");

            migrationBuilder.RenameIndex(
                name: "IX_LinkTokens_UserId",
                table: "link_tokens",
                newName: "IX_link_tokens_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_LinkTokens_ExpiresAt",
                table: "link_tokens",
                newName: "IX_link_tokens_ExpiresAt");

            migrationBuilder.RenameIndex(
                name: "IX_LinkTokens_UserId_IsUsed_ExpiresAt",
                table: "link_tokens",
                newName: "IX_link_tokens_UserId_IsUsed_ExpiresAt");
        }
    }
}
