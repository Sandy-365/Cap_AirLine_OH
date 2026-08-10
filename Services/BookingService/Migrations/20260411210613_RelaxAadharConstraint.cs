using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingService.Migrations
{
    /// <inheritdoc />
    public partial class RelaxAadharConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Passengers_AadharCardNo",
                table: "Passengers");

            migrationBuilder.CreateIndex(
                name: "IX_Passengers_AadharCardNo",
                table: "Passengers",
                column: "AadharCardNo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Passengers_AadharCardNo",
                table: "Passengers");

            migrationBuilder.CreateIndex(
                name: "IX_Passengers_AadharCardNo",
                table: "Passengers",
                column: "AadharCardNo",
                unique: true);
        }
    }
}
