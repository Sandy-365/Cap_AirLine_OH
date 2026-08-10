using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PassengerService.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSavedPassengerFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DietaryRequirements",
                table: "SavedPassengers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MedicalAlerts",
                table: "SavedPassengers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MedicalNeeds",
                table: "SavedPassengers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Nationality",
                table: "SavedPassengers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PassportNumber",
                table: "SavedPassengers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DietaryRequirements",
                table: "SavedPassengers");

            migrationBuilder.DropColumn(
                name: "MedicalAlerts",
                table: "SavedPassengers");

            migrationBuilder.DropColumn(
                name: "MedicalNeeds",
                table: "SavedPassengers");

            migrationBuilder.DropColumn(
                name: "Nationality",
                table: "SavedPassengers");

            migrationBuilder.DropColumn(
                name: "PassportNumber",
                table: "SavedPassengers");
        }
    }
}
