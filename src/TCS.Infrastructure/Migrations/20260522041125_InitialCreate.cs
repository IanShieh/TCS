using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TCS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Database already exists with correct schema — no DDL needed.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TCSMB");

            migrationBuilder.DropTable(
                name: "TCSTB");

            migrationBuilder.DropTable(
                name: "TCSTA");

            migrationBuilder.DropTable(
                name: "TCSMA");
        }
    }
}
