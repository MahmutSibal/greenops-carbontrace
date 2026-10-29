using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GreenOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SchemaIntegrityAndEmissionAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Ön kontrol: yeni unique index'ler ve kısaltılan sütunlarla çakışan veri varsa
            // migration'ı veri silmeden, açık bir mesajla durdur.
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM ConversionFactors GROUP BY CategoryId, [Year] HAVING COUNT(*) > 1)
    THROW 50001, 'ConversionFactors: ayni CategoryId+Year icin birden fazla katsayi var. Once tekillestirin.', 1;
IF EXISTS (SELECT 1 FROM AlertSettings GROUP BY CompanyId, AlertType HAVING COUNT(*) > 1)
    THROW 50002, 'AlertSettings: ayni CompanyId+AlertType icin birden fazla kayit var. Once tekillestirin.', 1;
IF EXISTS (SELECT 1 FROM Facilities WHERE IsActive = 1 GROUP BY CompanyId, Name HAVING COUNT(*) > 1)
    THROW 50003, 'Facilities: ayni sirkette ayni adli birden fazla aktif tesis var. Once yeniden adlandirin.', 1;
IF EXISTS (SELECT 1 FROM Companies WHERE TaxNo <> '' GROUP BY TaxNo HAVING COUNT(*) > 1)
    THROW 50004, 'Companies: ayni vergi numarasina sahip birden fazla sirket var. Once duzeltin.', 1;
");

            migrationBuilder.DropForeignKey(
                name: "FK_AlertSettings_Companies_CompanyId",
                table: "AlertSettings");

            migrationBuilder.DropForeignKey(
                name: "FK_ApiKeys_Companies_CompanyId",
                table: "ApiKeys");

            migrationBuilder.DropForeignKey(
                name: "FK_CarbonCredits_Companies_CompanyId",
                table: "CarbonCredits");

            migrationBuilder.DropForeignKey(
                name: "FK_ConversionFactors_EmissionCategories_CategoryId",
                table: "ConversionFactors");

            migrationBuilder.DropForeignKey(
                name: "FK_Documents_Companies_CompanyId",
                table: "Documents");

            migrationBuilder.DropForeignKey(
                name: "FK_EmissionEntries_EmissionCategories_CategoryId",
                table: "EmissionEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_EmissionEntries_Facilities_FacilityId",
                table: "EmissionEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_EmissionEntries_Users_CreatedByUserId",
                table: "EmissionEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_Facilities_Companies_CompanyId",
                table: "Facilities");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Companies_CompanyId",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_IotDevices_Facilities_FacilityId",
                table: "IotDevices");

            migrationBuilder.DropForeignKey(
                name: "FK_IotMeterReadings_IotDevices_IotDeviceId",
                table: "IotMeterReadings");

            migrationBuilder.DropForeignKey(
                name: "FK_Licenses_Companies_CompanyId",
                table: "Licenses");

            migrationBuilder.DropForeignKey(
                name: "FK_Reports_Companies_CompanyId",
                table: "Reports");

            migrationBuilder.DropForeignKey(
                name: "FK_SupplierEmissions_Suppliers_SupplierId",
                table: "SupplierEmissions");

            migrationBuilder.DropForeignKey(
                name: "FK_Suppliers_Companies_CompanyId",
                table: "Suppliers");

            migrationBuilder.DropForeignKey(
                name: "FK_Targets_Companies_CompanyId",
                table: "Targets");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Facilities_CompanyId",
                table: "Facilities");

            migrationBuilder.DropIndex(
                name: "IX_EmissionEntries_FacilityId",
                table: "EmissionEntries");

            migrationBuilder.DropIndex(
                name: "IX_ConversionFactors_CategoryId",
                table: "ConversionFactors");

            migrationBuilder.DropIndex(
                name: "IX_AlertSettings_CompanyId",
                table: "AlertSettings");

            migrationBuilder.AlterColumn<string>(
                name: "TwoFactorSecret",
                table: "Users",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PhoneNumber",
                table: "Users",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PasswordResetToken",
                table: "Users",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "Users",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "AvatarUrl",
                table: "Users",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ContactPhone",
                table: "Suppliers",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                table: "Suppliers",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PlanName",
                table: "Licenses",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Department",
                table: "Facilities",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                table: "Facilities",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RejectionReason",
                table: "EmissionEntries",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "EmissionEntries",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ConversionFactorId",
                table: "EmissionEntries",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FactorValue",
                table: "EmissionEntries",
                type: "decimal(18,6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PeriodEnd",
                table: "EmissionEntries",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PeriodStart",
                table: "EmissionEntries",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RejectedAt",
                table: "EmissionEntries",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RejectedByUserId",
                table: "EmissionEntries",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "EmissionEntries",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "EmissionEntries",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Factor",
                table: "ConversionFactors",
                type: "decimal(18,6)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<string>(
                name: "Industry",
                table: "Companies",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                table: "Companies",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "BillingPlans",
                keyColumn: "Id",
                keyValue: new Guid("b0000001-0000-0000-0000-000000000001"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 2, 26, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "BillingPlans",
                keyColumn: "Id",
                keyValue: new Guid("b0000001-0000-0000-0000-000000000002"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 2, 26, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "BillingPlans",
                keyColumn: "Id",
                keyValue: new Guid("b0000001-0000-0000-0000-000000000003"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 2, 26, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "BillingPlans",
                keyColumn: "Id",
                keyValue: new Guid("b0000001-0000-0000-0000-000000000004"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 2, 26, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("c0000001-0000-0000-0000-000000000001"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 2, 26, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("c0000001-0000-0000-0000-000000000002"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 2, 26, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("c0000001-0000-0000-0000-000000000003"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 2, 26, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("c0000001-0000-0000-0000-000000000004"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 2, 26, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            // Mevcut emisyon kayıtlarını yeni denetim alanlarıyla doldur.
            migrationBuilder.Sql(@"
UPDATE e SET e.CompanyId = f.CompanyId
FROM EmissionEntries e JOIN Facilities f ON f.Id = e.FacilityId;

UPDATE e SET e.Unit = c.Unit
FROM EmissionEntries e JOIN EmissionCategories c ON c.Id = e.CategoryId;

UPDATE e SET e.ConversionFactorId = cf.Id, e.FactorValue = cf.Factor
FROM EmissionEntries e
JOIN ConversionFactors cf ON cf.CategoryId = e.CategoryId AND cf.[Year] = YEAR(e.CreatedAt);
");

            migrationBuilder.CreateIndex(
                name: "IX_Users_PasswordResetToken",
                table: "Users",
                column: "PasswordResetToken",
                filter: "[PasswordResetToken] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Token",
                table: "RefreshTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_IsRead",
                table: "Notifications",
                columns: new[] { "UserId", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_Facilities_CompanyId_Name",
                table: "Facilities",
                columns: new[] { "CompanyId", "Name" },
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_EmissionEntries_CompanyId_Status_IsDeleted_CreatedAt",
                table: "EmissionEntries",
                columns: new[] { "CompanyId", "Status", "IsDeleted", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_EmissionEntries_ConversionFactorId",
                table: "EmissionEntries",
                column: "ConversionFactorId");

            migrationBuilder.CreateIndex(
                name: "IX_EmissionEntries_FacilityId_Status_IsDeleted_CreatedAt",
                table: "EmissionEntries",
                columns: new[] { "FacilityId", "Status", "IsDeleted", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_EmissionEntries_RejectedByUserId",
                table: "EmissionEntries",
                column: "RejectedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ConversionFactors_CategoryId_Year",
                table: "ConversionFactors",
                columns: new[] { "CategoryId", "Year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Companies_TaxNo",
                table: "Companies",
                column: "TaxNo",
                unique: true,
                filter: "[TaxNo] <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_CompanyId_CreatedAt",
                table: "AuditLogs",
                columns: new[] { "CompanyId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AlertSettings_CompanyId_AlertType",
                table: "AlertSettings",
                columns: new[] { "CompanyId", "AlertType" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AlertSettings_Companies_CompanyId",
                table: "AlertSettings",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ApiKeys_Companies_CompanyId",
                table: "ApiKeys",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CarbonCredits_Companies_CompanyId",
                table: "CarbonCredits",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ConversionFactors_EmissionCategories_CategoryId",
                table: "ConversionFactors",
                column: "CategoryId",
                principalTable: "EmissionCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_Companies_CompanyId",
                table: "Documents",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EmissionEntries_Companies_CompanyId",
                table: "EmissionEntries",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EmissionEntries_ConversionFactors_ConversionFactorId",
                table: "EmissionEntries",
                column: "ConversionFactorId",
                principalTable: "ConversionFactors",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_EmissionEntries_EmissionCategories_CategoryId",
                table: "EmissionEntries",
                column: "CategoryId",
                principalTable: "EmissionCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EmissionEntries_Facilities_FacilityId",
                table: "EmissionEntries",
                column: "FacilityId",
                principalTable: "Facilities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EmissionEntries_Users_CreatedByUserId",
                table: "EmissionEntries",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EmissionEntries_Users_RejectedByUserId",
                table: "EmissionEntries",
                column: "RejectedByUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Facilities_Companies_CompanyId",
                table: "Facilities",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Companies_CompanyId",
                table: "Invoices",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_IotDevices_Facilities_FacilityId",
                table: "IotDevices",
                column: "FacilityId",
                principalTable: "Facilities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_IotMeterReadings_IotDevices_IotDeviceId",
                table: "IotMeterReadings",
                column: "IotDeviceId",
                principalTable: "IotDevices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Licenses_Companies_CompanyId",
                table: "Licenses",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_Companies_CompanyId",
                table: "Reports",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierEmissions_Suppliers_SupplierId",
                table: "SupplierEmissions",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Suppliers_Companies_CompanyId",
                table: "Suppliers",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Targets_Companies_CompanyId",
                table: "Targets",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AlertSettings_Companies_CompanyId",
                table: "AlertSettings");

            migrationBuilder.DropForeignKey(
                name: "FK_ApiKeys_Companies_CompanyId",
                table: "ApiKeys");

            migrationBuilder.DropForeignKey(
                name: "FK_CarbonCredits_Companies_CompanyId",
                table: "CarbonCredits");

            migrationBuilder.DropForeignKey(
                name: "FK_ConversionFactors_EmissionCategories_CategoryId",
                table: "ConversionFactors");

            migrationBuilder.DropForeignKey(
                name: "FK_Documents_Companies_CompanyId",
                table: "Documents");

            migrationBuilder.DropForeignKey(
                name: "FK_EmissionEntries_Companies_CompanyId",
                table: "EmissionEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_EmissionEntries_ConversionFactors_ConversionFactorId",
                table: "EmissionEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_EmissionEntries_EmissionCategories_CategoryId",
                table: "EmissionEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_EmissionEntries_Facilities_FacilityId",
                table: "EmissionEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_EmissionEntries_Users_CreatedByUserId",
                table: "EmissionEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_EmissionEntries_Users_RejectedByUserId",
                table: "EmissionEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_Facilities_Companies_CompanyId",
                table: "Facilities");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Companies_CompanyId",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_IotDevices_Facilities_FacilityId",
                table: "IotDevices");

            migrationBuilder.DropForeignKey(
                name: "FK_IotMeterReadings_IotDevices_IotDeviceId",
                table: "IotMeterReadings");

            migrationBuilder.DropForeignKey(
                name: "FK_Licenses_Companies_CompanyId",
                table: "Licenses");

            migrationBuilder.DropForeignKey(
                name: "FK_Reports_Companies_CompanyId",
                table: "Reports");

            migrationBuilder.DropForeignKey(
                name: "FK_SupplierEmissions_Suppliers_SupplierId",
                table: "SupplierEmissions");

            migrationBuilder.DropForeignKey(
                name: "FK_Suppliers_Companies_CompanyId",
                table: "Suppliers");

            migrationBuilder.DropForeignKey(
                name: "FK_Targets_Companies_CompanyId",
                table: "Targets");

            migrationBuilder.DropIndex(
                name: "IX_Users_PasswordResetToken",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_Token",
                table: "RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_UserId_IsRead",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Facilities_CompanyId_Name",
                table: "Facilities");

            migrationBuilder.DropIndex(
                name: "IX_EmissionEntries_CompanyId_Status_IsDeleted_CreatedAt",
                table: "EmissionEntries");

            migrationBuilder.DropIndex(
                name: "IX_EmissionEntries_ConversionFactorId",
                table: "EmissionEntries");

            migrationBuilder.DropIndex(
                name: "IX_EmissionEntries_FacilityId_Status_IsDeleted_CreatedAt",
                table: "EmissionEntries");

            migrationBuilder.DropIndex(
                name: "IX_EmissionEntries_RejectedByUserId",
                table: "EmissionEntries");

            migrationBuilder.DropIndex(
                name: "IX_ConversionFactors_CategoryId_Year",
                table: "ConversionFactors");

            migrationBuilder.DropIndex(
                name: "IX_Companies_TaxNo",
                table: "Companies");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_CompanyId_CreatedAt",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AlertSettings_CompanyId_AlertType",
                table: "AlertSettings");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "EmissionEntries");

            migrationBuilder.DropColumn(
                name: "ConversionFactorId",
                table: "EmissionEntries");

            migrationBuilder.DropColumn(
                name: "FactorValue",
                table: "EmissionEntries");

            migrationBuilder.DropColumn(
                name: "PeriodEnd",
                table: "EmissionEntries");

            migrationBuilder.DropColumn(
                name: "PeriodStart",
                table: "EmissionEntries");

            migrationBuilder.DropColumn(
                name: "RejectedAt",
                table: "EmissionEntries");

            migrationBuilder.DropColumn(
                name: "RejectedByUserId",
                table: "EmissionEntries");

            migrationBuilder.DropColumn(
                name: "Unit",
                table: "EmissionEntries");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "EmissionEntries");

            migrationBuilder.AlterColumn<string>(
                name: "TwoFactorSecret",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PhoneNumber",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PasswordResetToken",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "AvatarUrl",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ContactPhone",
                table: "Suppliers",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                table: "Suppliers",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PlanName",
                table: "Licenses",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Department",
                table: "Facilities",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                table: "Facilities",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RejectionReason",
                table: "EmissionEntries",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Factor",
                table: "ConversionFactors",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,6)");

            migrationBuilder.AlterColumn<string>(
                name: "Industry",
                table: "Companies",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                table: "Companies",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "BillingPlans",
                keyColumn: "Id",
                keyValue: new Guid("b0000001-0000-0000-0000-000000000001"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 3, 5, 0, 50, 59, 786, DateTimeKind.Unspecified).AddTicks(6358), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "BillingPlans",
                keyColumn: "Id",
                keyValue: new Guid("b0000001-0000-0000-0000-000000000002"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 3, 5, 0, 50, 59, 786, DateTimeKind.Unspecified).AddTicks(6368), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "BillingPlans",
                keyColumn: "Id",
                keyValue: new Guid("b0000001-0000-0000-0000-000000000003"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 3, 5, 0, 50, 59, 786, DateTimeKind.Unspecified).AddTicks(6375), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "BillingPlans",
                keyColumn: "Id",
                keyValue: new Guid("b0000001-0000-0000-0000-000000000004"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 3, 5, 0, 50, 59, 786, DateTimeKind.Unspecified).AddTicks(6380), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("c0000001-0000-0000-0000-000000000001"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 3, 5, 0, 50, 59, 789, DateTimeKind.Unspecified).AddTicks(1970), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("c0000001-0000-0000-0000-000000000002"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 3, 5, 0, 50, 59, 789, DateTimeKind.Unspecified).AddTicks(1977), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("c0000001-0000-0000-0000-000000000003"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 3, 5, 0, 50, 59, 789, DateTimeKind.Unspecified).AddTicks(1983), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("c0000001-0000-0000-0000-000000000004"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 3, 5, 0, 50, 59, 789, DateTimeKind.Unspecified).AddTicks(1989), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Facilities_CompanyId",
                table: "Facilities",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_EmissionEntries_FacilityId",
                table: "EmissionEntries",
                column: "FacilityId");

            migrationBuilder.CreateIndex(
                name: "IX_ConversionFactors_CategoryId",
                table: "ConversionFactors",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertSettings_CompanyId",
                table: "AlertSettings",
                column: "CompanyId");

            migrationBuilder.AddForeignKey(
                name: "FK_AlertSettings_Companies_CompanyId",
                table: "AlertSettings",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ApiKeys_Companies_CompanyId",
                table: "ApiKeys",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CarbonCredits_Companies_CompanyId",
                table: "CarbonCredits",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ConversionFactors_EmissionCategories_CategoryId",
                table: "ConversionFactors",
                column: "CategoryId",
                principalTable: "EmissionCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_Companies_CompanyId",
                table: "Documents",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_EmissionEntries_EmissionCategories_CategoryId",
                table: "EmissionEntries",
                column: "CategoryId",
                principalTable: "EmissionCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_EmissionEntries_Facilities_FacilityId",
                table: "EmissionEntries",
                column: "FacilityId",
                principalTable: "Facilities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_EmissionEntries_Users_CreatedByUserId",
                table: "EmissionEntries",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Facilities_Companies_CompanyId",
                table: "Facilities",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Companies_CompanyId",
                table: "Invoices",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_IotDevices_Facilities_FacilityId",
                table: "IotDevices",
                column: "FacilityId",
                principalTable: "Facilities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_IotMeterReadings_IotDevices_IotDeviceId",
                table: "IotMeterReadings",
                column: "IotDeviceId",
                principalTable: "IotDevices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Licenses_Companies_CompanyId",
                table: "Licenses",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_Companies_CompanyId",
                table: "Reports",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierEmissions_Suppliers_SupplierId",
                table: "SupplierEmissions",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Suppliers_Companies_CompanyId",
                table: "Suppliers",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Targets_Companies_CompanyId",
                table: "Targets",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
