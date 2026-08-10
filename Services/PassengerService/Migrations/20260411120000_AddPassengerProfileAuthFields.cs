using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PassengerService.Migrations
{
    public partial class AddPassengerProfileAuthFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "PassengerProfiles",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "PassengerProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "PassengerProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "PassengerProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "Passenger");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "PassengerProfiles",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsEmailVerified",
                table: "PassengerProfiles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "VerificationToken",
                table: "PassengerProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VerificationTokenExpiry",
                table: "PassengerProfiles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResetToken",
                table: "PassengerProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResetTokenExpiry",
                table: "PassengerProfiles",
                type: "datetime2",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Email",
                table: "PassengerProfiles");

            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "PassengerProfiles");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "PassengerProfiles");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "PassengerProfiles");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "PassengerProfiles");

            migrationBuilder.DropColumn(
                name: "IsEmailVerified",
                table: "PassengerProfiles");

            migrationBuilder.DropColumn(
                name: "VerificationToken",
                table: "PassengerProfiles");

            migrationBuilder.DropColumn(
                name: "VerificationTokenExpiry",
                table: "PassengerProfiles");

            migrationBuilder.DropColumn(
                name: "ResetToken",
                table: "PassengerProfiles");

            migrationBuilder.DropColumn(
                name: "ResetTokenExpiry",
                table: "PassengerProfiles");
        }
    }
}
