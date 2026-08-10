using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PassengerService.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreatePassengerDB : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PassengerProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PassportNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Nationality = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FrequentFlyerNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DietaryRequirements = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PreferredSeatLocation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PassengerProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Rewards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Points = table.Column<int>(type: "int", nullable: false),
                    TransactionType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BookingId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rewards", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Rewards_UserId",
                table: "Rewards",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PassengerProfiles");

            migrationBuilder.DropTable(
                name: "Rewards");
        }
    }
}
