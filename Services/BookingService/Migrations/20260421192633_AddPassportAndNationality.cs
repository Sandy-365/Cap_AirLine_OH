using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingService.Migrations
{
    /// <inheritdoc />
    public partial class AddPassportAndNationality : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Nationality",
                table: "Passengers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PassportNumber",
                table: "Passengers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Nationality",
                table: "Passengers");

            migrationBuilder.DropColumn(
                name: "PassportNumber",
                table: "Passengers");
        }
    }
}
