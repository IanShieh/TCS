using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TCS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPlantToTrainingHeader : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Plant",
                table: "TRNF01",
                type: "char(6)",
                unicode: false,
                fixedLength: true,
                maxLength: 6,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Plant",
                table: "TRNF01");
        }
    }
}
