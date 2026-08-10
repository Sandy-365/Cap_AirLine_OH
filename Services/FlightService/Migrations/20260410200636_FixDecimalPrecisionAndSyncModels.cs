using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlightService.Migrations
{
    /// <inheritdoc />
    public partial class FixDecimalPrecisionAndSyncModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "FlightSchedules",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Flights",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "FlightSchedules");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Flights");
        }
    }
}
