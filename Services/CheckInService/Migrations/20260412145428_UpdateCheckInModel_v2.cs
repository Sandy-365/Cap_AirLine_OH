using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CheckInService.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCheckInModel_v2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CheckIns_BookingId",
                table: "CheckIns");

            migrationBuilder.CreateIndex(
                name: "IX_CheckIns_BookingId",
                table: "CheckIns",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_CheckIns_PassengerId",
                table: "CheckIns",
                column: "PassengerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CheckIns_BookingId",
                table: "CheckIns");

            migrationBuilder.DropIndex(
                name: "IX_CheckIns_PassengerId",
                table: "CheckIns");

            migrationBuilder.CreateIndex(
                name: "IX_CheckIns_BookingId",
                table: "CheckIns",
                column: "BookingId",
                unique: true);
        }
    }
}
