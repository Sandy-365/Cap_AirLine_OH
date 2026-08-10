using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingService.Migrations
{
    /// <inheritdoc />
    public partial class AddPassengerFare : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Fare",
                table: "Passengers",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Fare",
                table: "Passengers");
        }
    }
}
