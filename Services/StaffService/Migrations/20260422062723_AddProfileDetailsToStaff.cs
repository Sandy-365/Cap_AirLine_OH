using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StaffService.Migrations
{
    /// <inheritdoc />
    public partial class AddProfileDetailsToStaff : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AadharNumber",
                table: "StaffProfiles",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateOfBirth",
                table: "StaffProfiles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Gender",
                table: "StaffProfiles",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasChangedPassword",
                table: "StaffProfiles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsProfileComplete",
                table: "StaffProfiles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Nationality",
                table: "StaffProfiles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PassportNumber",
                table: "StaffProfiles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "StaffProfiles",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AadharNumber",
                table: "StaffProfiles");

            migrationBuilder.DropColumn(
                name: "DateOfBirth",
                table: "StaffProfiles");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "StaffProfiles");

            migrationBuilder.DropColumn(
                name: "HasChangedPassword",
                table: "StaffProfiles");

            migrationBuilder.DropColumn(
                name: "IsProfileComplete",
                table: "StaffProfiles");

            migrationBuilder.DropColumn(
                name: "Nationality",
                table: "StaffProfiles");

            migrationBuilder.DropColumn(
                name: "PassportNumber",
                table: "StaffProfiles");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "StaffProfiles");
        }
    }
}
