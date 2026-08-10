using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PassengerService.Migrations
{
    /// <inheritdoc />
    public partial class AddTravelPreferencesAndSavedPassengersFinal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PreferredSeatLocation",
                table: "PassengerProfiles",
                newName: "MedicalNeeds");

            migrationBuilder.RenameColumn(
                name: "FrequentFlyerNumber",
                table: "PassengerProfiles",
                newName: "MedicalAlerts");

            migrationBuilder.AddColumn<string>(
                name: "Gender",
                table: "PassengerProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "SavedPassengers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PassengerProfileId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Age = table.Column<int>(type: "int", nullable: false),
                    Gender = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Aadhar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedPassengers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SavedPassengers_PassengerProfiles_PassengerProfileId",
                        column: x => x.PassengerProfileId,
                        principalTable: "PassengerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SavedPassengers_PassengerProfileId",
                table: "SavedPassengers",
                column: "PassengerProfileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SavedPassengers");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "PassengerProfiles");

            migrationBuilder.RenameColumn(
                name: "MedicalNeeds",
                table: "PassengerProfiles",
                newName: "PreferredSeatLocation");

            migrationBuilder.RenameColumn(
                name: "MedicalAlerts",
                table: "PassengerProfiles",
                newName: "FrequentFlyerNumber");
        }
    }
}
