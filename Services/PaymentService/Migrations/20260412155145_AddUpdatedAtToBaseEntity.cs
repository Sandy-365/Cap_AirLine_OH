using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PaymentService.Migrations
{
    /// <inheritdoc />
    public partial class AddUpdatedAtToBaseEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Payments]') AND name = 'UpdatedAt') " +
                                "BEGIN ALTER TABLE [Payments] ADD [UpdatedAt] datetime2 NULL; END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Payments]') AND name = 'UpdatedAt') " +
                                "BEGIN ALTER TABLE [Payments] DROP COLUMN [UpdatedAt]; END");
        }
    }
}
