using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdminService.Migrations
{
    /// <inheritdoc />
    public partial class AddVisitorLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VisitorLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Identity = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PageUrl = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    SearchTerm = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FlightId = table.Column<int>(type: "int", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisitorLogs", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VisitorLogs");
        }
    }
}
