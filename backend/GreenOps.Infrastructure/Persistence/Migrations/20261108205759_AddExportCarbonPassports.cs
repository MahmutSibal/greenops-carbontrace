using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GreenOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExportCarbonPassports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExportCarbonPassports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ProductCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ShipmentReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    QuantityUnit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ProductionStart = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ProductionEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TotalEmissionsKg = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    EmissionsPerUnitKg = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    DataReliabilityPercent = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    PublicTokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExportCarbonPassports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExportCarbonPassports_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExportCarbonPassportEntries",
                columns: table => new
                {
                    PassportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmissionEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExportCarbonPassportEntries", x => new { x.PassportId, x.EmissionEntryId });
                    table.ForeignKey(
                        name: "FK_ExportCarbonPassportEntries_EmissionEntries_EmissionEntryId",
                        column: x => x.EmissionEntryId,
                        principalTable: "EmissionEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExportCarbonPassportEntries_ExportCarbonPassports_PassportId",
                        column: x => x.PassportId,
                        principalTable: "ExportCarbonPassports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExportCarbonPassportEntries_EmissionEntryId",
                table: "ExportCarbonPassportEntries",
                column: "EmissionEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_ExportCarbonPassports_CompanyId",
                table: "ExportCarbonPassports",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_ExportCarbonPassports_PublicTokenHash",
                table: "ExportCarbonPassports",
                column: "PublicTokenHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExportCarbonPassportEntries");

            migrationBuilder.DropTable(
                name: "ExportCarbonPassports");
        }
    }
}
