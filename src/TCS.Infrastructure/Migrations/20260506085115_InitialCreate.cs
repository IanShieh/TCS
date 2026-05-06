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
            migrationBuilder.CreateTable(
                name: "TRNM01",
                columns: table => new
                {
                    LicenseType = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Category = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    Hours = table.Column<int>(type: "int", nullable: true),
                    Years = table.Column<int>(type: "int", nullable: true),
                    Creator = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    CreateDate = table.Column<string>(type: "char(8)", unicode: false, fixedLength: true, maxLength: 8, nullable: true),
                    Modifier = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    ModiDate = table.Column<string>(type: "char(8)", unicode: false, fixedLength: true, maxLength: 8, nullable: true),
                    Flag = table.Column<decimal>(type: "decimal(1,0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TRNM01", x => x.LicenseType);
                });

            migrationBuilder.CreateTable(
                name: "TRNF01",
                columns: table => new
                {
                    EmployeeId = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    LicenseType = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    RequiredHours = table.Column<int>(type: "int", nullable: false),
                    Remark = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Creator = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    CreateDate = table.Column<string>(type: "char(8)", unicode: false, fixedLength: true, maxLength: 8, nullable: true),
                    Modifier = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    ModiDate = table.Column<string>(type: "char(8)", unicode: false, fixedLength: true, maxLength: 8, nullable: true),
                    Flag = table.Column<decimal>(type: "decimal(1,0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TRNF01", x => new { x.EmployeeId, x.LicenseType });
                    table.ForeignKey(
                        name: "FK_TRNF01_TRNM01_LicenseType",
                        column: x => x.LicenseType,
                        principalTable: "TRNM01",
                        principalColumn: "LicenseType",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TRNM02",
                columns: table => new
                {
                    LicenseType = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Plant = table.Column<string>(type: "char(10)", unicode: false, fixedLength: true, maxLength: 10, nullable: false),
                    RequiredCount = table.Column<int>(type: "int", nullable: false),
                    Creator = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    CreateDate = table.Column<string>(type: "char(8)", unicode: false, fixedLength: true, maxLength: 8, nullable: true),
                    Modifier = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    ModiDate = table.Column<string>(type: "char(8)", unicode: false, fixedLength: true, maxLength: 8, nullable: true),
                    Flag = table.Column<decimal>(type: "decimal(1,0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TRNM02", x => new { x.LicenseType, x.Plant });
                    table.ForeignKey(
                        name: "FK_TRNM02_TRNM01_LicenseType",
                        column: x => x.LicenseType,
                        principalTable: "TRNM01",
                        principalColumn: "LicenseType",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TRNF02",
                columns: table => new
                {
                    EmployeeId = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    LicenseType = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    TrainingDate = table.Column<DateTime>(type: "date", nullable: false),
                    TrainingType = table.Column<int>(type: "int", nullable: false),
                    Hours = table.Column<decimal>(type: "decimal(6,1)", nullable: false),
                    IsExpired = table.Column<bool>(type: "bit", nullable: false),
                    Creator = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    CreateDate = table.Column<string>(type: "char(8)", unicode: false, fixedLength: true, maxLength: 8, nullable: true),
                    Modifier = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    ModiDate = table.Column<string>(type: "char(8)", unicode: false, fixedLength: true, maxLength: 8, nullable: true),
                    Flag = table.Column<decimal>(type: "decimal(1,0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TRNF02", x => new { x.EmployeeId, x.LicenseType, x.TrainingDate });
                    table.ForeignKey(
                        name: "FK_TRNF02_TRNF01_EmployeeId_LicenseType",
                        columns: x => new { x.EmployeeId, x.LicenseType },
                        principalTable: "TRNF01",
                        principalColumns: new[] { "EmployeeId", "LicenseType" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TRNF01_LicenseType",
                table: "TRNF01",
                column: "LicenseType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TRNF02");

            migrationBuilder.DropTable(
                name: "TRNM02");

            migrationBuilder.DropTable(
                name: "TRNF01");

            migrationBuilder.DropTable(
                name: "TRNM01");
        }
    }
}
