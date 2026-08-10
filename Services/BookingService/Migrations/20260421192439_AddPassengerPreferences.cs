using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingService.Migrations
{
    /// <inheritdoc />
    public partial class AddPassengerPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DietaryRequirements",
                table: "Passengers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MedicalAlerts",
                table: "Passengers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MedicalNeeds",
                table: "Passengers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DietaryRequirements",
                table: "Passengers");

            migrationBuilder.DropColumn(
                name: "MedicalAlerts",
                table: "Passengers");

            migrationBuilder.DropColumn(
                name: "MedicalNeeds",
                table: "Passengers");
        }
    }
}
