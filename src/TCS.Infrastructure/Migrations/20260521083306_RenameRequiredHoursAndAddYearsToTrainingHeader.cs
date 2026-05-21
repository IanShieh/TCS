using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TCS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameRequiredHoursAndAddYearsToTrainingHeader : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RequiredHours",
                table: "TCSTA",
                newName: "Hours");

            migrationBuilder.AddColumn<int>(
                name: "Years",
                table: "TCSTA",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Years",
                table: "TCSTA");

            migrationBuilder.RenameColumn(
                name: "Hours",
                table: "TCSTA",
                newName: "RequiredHours");
        }
    }
}
