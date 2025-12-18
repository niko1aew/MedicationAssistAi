using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedicationAssist.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPendingFieldsToReminders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PendingFirstSentAt",
                table: "Reminders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PendingLastSentAt",
                table: "Reminders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PendingMessageId",
                table: "Reminders",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PendingUntil",
                table: "Reminders",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PendingFirstSentAt",
                table: "Reminders");

            migrationBuilder.DropColumn(
                name: "PendingLastSentAt",
                table: "Reminders");

            migrationBuilder.DropColumn(
                name: "PendingMessageId",
                table: "Reminders");

            migrationBuilder.DropColumn(
                name: "PendingUntil",
                table: "Reminders");
        }
    }
}
