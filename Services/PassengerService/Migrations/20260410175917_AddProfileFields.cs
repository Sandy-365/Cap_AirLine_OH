using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PassengerService.Migrations
{
    /// <inheritdoc />
    public partial class AddProfileFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Rewards",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DateOfBirth",
                table: "PassengerProfiles",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<string>(
                name: "Aadhar",
                table: "PassengerProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "PassengerProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "PassengerProfiles",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Rewards");

            migrationBuilder.DropColumn(
                name: "Aadhar",
                table: "PassengerProfiles");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "PassengerProfiles");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "PassengerProfiles");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DateOfBirth",
                table: "PassengerProfiles",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);
        }
    }
}
