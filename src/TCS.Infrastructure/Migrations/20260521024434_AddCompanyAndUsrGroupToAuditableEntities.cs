using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TCS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyAndUsrGroupToAuditableEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "COMPANY",
                table: "TRNM02",
                type: "varchar(10)",
                unicode: false,
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "USR_GROUP",
                table: "TRNM02",
                type: "varchar(10)",
                unicode: false,
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "COMPANY",
                table: "TRNM01",
                type: "varchar(10)",
                unicode: false,
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "USR_GROUP",
                table: "TRNM01",
                type: "varchar(10)",
                unicode: false,
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "COMPANY",
                table: "TRNF02",
                type: "varchar(10)",
                unicode: false,
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "USR_GROUP",
                table: "TRNF02",
                type: "varchar(10)",
                unicode: false,
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "COMPANY",
                table: "TRNF01",
                type: "varchar(10)",
                unicode: false,
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "USR_GROUP",
                table: "TRNF01",
                type: "varchar(10)",
                unicode: false,
                maxLength: 10,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "COMPANY",
                table: "TRNM02");

            migrationBuilder.DropColumn(
                name: "USR_GROUP",
                table: "TRNM02");

            migrationBuilder.DropColumn(
                name: "COMPANY",
                table: "TRNM01");

            migrationBuilder.DropColumn(
                name: "USR_GROUP",
                table: "TRNM01");

            migrationBuilder.DropColumn(
                name: "COMPANY",
                table: "TRNF02");

            migrationBuilder.DropColumn(
                name: "USR_GROUP",
                table: "TRNF02");

            migrationBuilder.DropColumn(
                name: "COMPANY",
                table: "TRNF01");

            migrationBuilder.DropColumn(
                name: "USR_GROUP",
                table: "TRNF01");
        }
    }
}
