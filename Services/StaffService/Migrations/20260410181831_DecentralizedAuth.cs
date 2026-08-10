using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StaffService.Migrations
{
    /// <inheritdoc />
    public partial class DecentralizedAuth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserId",
                table: "StaffProfiles");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "StaffProfiles",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "StaffProfiles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsEmailVerified",
                table: "StaffProfiles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "StaffProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "StaffProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ResetToken",
                table: "StaffProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResetTokenExpiry",
                table: "StaffProfiles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "StaffProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

/*  --- Column already exists in DB, skipping to avoid error ---
            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "StaffProfiles",
                type: "datetime2",
                nullable: true);
*/

            migrationBuilder.AddColumn<string>(
                name: "VerificationToken",
                table: "StaffProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VerificationTokenExpiry",
                table: "StaffProfiles",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Email",
                table: "StaffProfiles");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "StaffProfiles");

            migrationBuilder.DropColumn(
                name: "IsEmailVerified",
                table: "StaffProfiles");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "StaffProfiles");

            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "StaffProfiles");

            migrationBuilder.DropColumn(
                name: "ResetToken",
                table: "StaffProfiles");

            migrationBuilder.DropColumn(
                name: "ResetTokenExpiry",
                table: "StaffProfiles");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "StaffProfiles");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "StaffProfiles");

            migrationBuilder.DropColumn(
                name: "VerificationToken",
                table: "StaffProfiles");

            migrationBuilder.DropColumn(
                name: "VerificationTokenExpiry",
                table: "StaffProfiles");

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "StaffProfiles",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
