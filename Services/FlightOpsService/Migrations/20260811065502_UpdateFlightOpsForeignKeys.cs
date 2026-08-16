using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlightOpsService.Migrations
{
    /// <inheritdoc />
    public partial class UpdateFlightOpsForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_CheckIns_FlightId",
                table: "CheckIns",
                column: "FlightId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_FlightId",
                table: "Bookings",
                column: "FlightId");

            migrationBuilder.AddForeignKey(
                name: "FK_Baggages_Bookings_BookingId",
                table: "Baggages",
                column: "BookingId",
                principalTable: "Bookings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_FlightSchedules_ScheduleId",
                table: "Bookings",
                column: "ScheduleId",
                principalTable: "FlightSchedules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Flights_FlightId",
                table: "Bookings",
                column: "FlightId",
                principalTable: "Flights",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CheckIns_Bookings_BookingId",
                table: "CheckIns",
                column: "BookingId",
                principalTable: "Bookings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CheckIns_Flights_FlightId",
                table: "CheckIns",
                column: "FlightId",
                principalTable: "Flights",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CheckIns_Passengers_PassengerId",
                table: "CheckIns",
                column: "PassengerId",
                principalTable: "Passengers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Baggages_Bookings_BookingId",
                table: "Baggages");

            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_FlightSchedules_ScheduleId",
                table: "Bookings");

            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Flights_FlightId",
                table: "Bookings");

            migrationBuilder.DropForeignKey(
                name: "FK_CheckIns_Bookings_BookingId",
                table: "CheckIns");

            migrationBuilder.DropForeignKey(
                name: "FK_CheckIns_Flights_FlightId",
                table: "CheckIns");

            migrationBuilder.DropForeignKey(
                name: "FK_CheckIns_Passengers_PassengerId",
                table: "CheckIns");

            migrationBuilder.DropIndex(
                name: "IX_CheckIns_FlightId",
                table: "CheckIns");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_FlightId",
                table: "Bookings");
        }
    }
}
