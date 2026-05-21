using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TCS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameTablesFromTRNToTCS : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TRNF01_TRNM01_LicenseType",
                table: "TRNF01");

            migrationBuilder.DropForeignKey(
                name: "FK_TRNF02_TRNF01_EmployeeId_LicenseType",
                table: "TRNF02");

            migrationBuilder.DropForeignKey(
                name: "FK_TRNM02_TRNM01_LicenseType",
                table: "TRNM02");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TRNM02",
                table: "TRNM02");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TRNM01",
                table: "TRNM01");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TRNF02",
                table: "TRNF02");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TRNF01",
                table: "TRNF01");

            migrationBuilder.RenameTable(
                name: "TRNM02",
                newName: "TCSMB");

            migrationBuilder.RenameTable(
                name: "TRNM01",
                newName: "TCSMA");

            migrationBuilder.RenameTable(
                name: "TRNF02",
                newName: "TCSTB");

            migrationBuilder.RenameTable(
                name: "TRNF01",
                newName: "TCSTA");

            migrationBuilder.RenameIndex(
                name: "IX_TRNF01_LicenseType",
                table: "TCSTA",
                newName: "IX_TCSTA_LicenseType");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TCSMB",
                table: "TCSMB",
                columns: new[] { "LicenseType", "Plant" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_TCSMA",
                table: "TCSMA",
                column: "LicenseType");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TCSTB",
                table: "TCSTB",
                columns: new[] { "EmployeeId", "LicenseType", "TrainingDate" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_TCSTA",
                table: "TCSTA",
                columns: new[] { "EmployeeId", "LicenseType" });

            migrationBuilder.AddForeignKey(
                name: "FK_TCSMB_TCSMA_LicenseType",
                table: "TCSMB",
                column: "LicenseType",
                principalTable: "TCSMA",
                principalColumn: "LicenseType",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TCSTA_TCSMA_LicenseType",
                table: "TCSTA",
                column: "LicenseType",
                principalTable: "TCSMA",
                principalColumn: "LicenseType",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TCSTB_TCSTA_EmployeeId_LicenseType",
                table: "TCSTB",
                columns: new[] { "EmployeeId", "LicenseType" },
                principalTable: "TCSTA",
                principalColumns: new[] { "EmployeeId", "LicenseType" },
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TCSMB_TCSMA_LicenseType",
                table: "TCSMB");

            migrationBuilder.DropForeignKey(
                name: "FK_TCSTA_TCSMA_LicenseType",
                table: "TCSTA");

            migrationBuilder.DropForeignKey(
                name: "FK_TCSTB_TCSTA_EmployeeId_LicenseType",
                table: "TCSTB");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TCSTB",
                table: "TCSTB");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TCSTA",
                table: "TCSTA");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TCSMB",
                table: "TCSMB");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TCSMA",
                table: "TCSMA");

            migrationBuilder.RenameTable(
                name: "TCSTB",
                newName: "TRNF02");

            migrationBuilder.RenameTable(
                name: "TCSTA",
                newName: "TRNF01");

            migrationBuilder.RenameTable(
                name: "TCSMB",
                newName: "TRNM02");

            migrationBuilder.RenameTable(
                name: "TCSMA",
                newName: "TRNM01");

            migrationBuilder.RenameIndex(
                name: "IX_TCSTA_LicenseType",
                table: "TRNF01",
                newName: "IX_TRNF01_LicenseType");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TRNF02",
                table: "TRNF02",
                columns: new[] { "EmployeeId", "LicenseType", "TrainingDate" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_TRNF01",
                table: "TRNF01",
                columns: new[] { "EmployeeId", "LicenseType" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_TRNM02",
                table: "TRNM02",
                columns: new[] { "LicenseType", "Plant" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_TRNM01",
                table: "TRNM01",
                column: "LicenseType");

            migrationBuilder.AddForeignKey(
                name: "FK_TRNF01_TRNM01_LicenseType",
                table: "TRNF01",
                column: "LicenseType",
                principalTable: "TRNM01",
                principalColumn: "LicenseType",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TRNF02_TRNF01_EmployeeId_LicenseType",
                table: "TRNF02",
                columns: new[] { "EmployeeId", "LicenseType" },
                principalTable: "TRNF01",
                principalColumns: new[] { "EmployeeId", "LicenseType" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TRNM02_TRNM01_LicenseType",
                table: "TRNM02",
                column: "LicenseType",
                principalTable: "TRNM01",
                principalColumn: "LicenseType",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
