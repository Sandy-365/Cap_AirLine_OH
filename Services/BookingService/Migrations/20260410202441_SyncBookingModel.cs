using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingService.Migrations
{
    /// <inheritdoc />
    public partial class SyncBookingModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Passengers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserEmail",
                table: "Bookings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UserName",
                table: "Bookings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Passengers");

            migrationBuilder.DropColumn(
                name: "UserEmail",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "UserName",
                table: "Bookings");
        }
    }
}
