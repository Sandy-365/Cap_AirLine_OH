using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PassengerService.Migrations
{
    /// <inheritdoc />
    public partial class FixPendingChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserId",
                table: "PassengerProfiles");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "PassengerProfiles",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
