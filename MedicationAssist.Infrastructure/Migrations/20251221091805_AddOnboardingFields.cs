using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedicationAssist.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOnboardingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsOnboardingCompleted",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "OnboardingStep",
                table: "Users",
                type: "integer",
                nullable: true);

            // Set default values for existing users - they are considered as already familiar with the app
            migrationBuilder.Sql(@"
                UPDATE ""Users"" 
                SET ""IsOnboardingCompleted"" = true 
                WHERE ""CreatedAt"" < NOW();
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsOnboardingCompleted",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "OnboardingStep",
                table: "Users");
        }
    }
}
