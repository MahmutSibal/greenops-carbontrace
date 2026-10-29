using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GreenOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class VerifiedEmissionFactors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "ConversionFactors",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsVerified",
                table: "ConversionFactors",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "ConversionFactors",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceUrl",
                table: "ConversionFactors",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            // Yeni sütun mevcut satırlara false gelir; daha önce eklenmiş tüm katsayılar aktif kalmalı.
            // Kaynaksız elektrik yer tutucusu aşağıdaki seed güncellemesiyle ayrıca pasife alınır.
            migrationBuilder.Sql("UPDATE ConversionFactors SET IsActive = 1;");

            migrationBuilder.UpdateData(
                table: "ConversionFactors",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "IsActive", "IsVerified", "Source", "SourceUrl" },
                values: new object[] { false, false, "Geliştirme yer tutucusu — resmi bir kaynağa dayanmaz (bkz. KAYNAKLAR.md bölüm 5.2). ETKB 2023 resmi değeriyle değiştirildi.", null });

            migrationBuilder.UpdateData(
                table: "ConversionFactors",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                columns: new[] { "IsActive", "IsVerified", "Source", "SourceUrl" },
                values: new object[] { true, false, "Geliştirme yer tutucusu — resmi bir kaynağa dayanmaz (bkz. KAYNAKLAR.md bölüm 5.2).", null });

            migrationBuilder.UpdateData(
                table: "ConversionFactors",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                columns: new[] { "IsActive", "IsVerified", "Source", "SourceUrl" },
                values: new object[] { true, false, "Geliştirme yer tutucusu — resmi bir kaynağa dayanmaz (bkz. KAYNAKLAR.md bölüm 5.2).", null });

            migrationBuilder.InsertData(
                table: "ConversionFactors",
                columns: new[] { "Id", "CategoryId", "Factor", "IsActive", "IsVerified", "Source", "SourceUrl", "Year" },
                values: new object[] { new Guid("77777777-7777-7777-7777-777777777777"), new Guid("11111111-1111-1111-1111-111111111111"), 0.469m, true, true, "T.C. Enerji ve Tabii Kaynaklar Bakanlığı, Türkiye Elektrik Üretimi ve Elektrik Tüketim Noktası Emisyon Faktörleri Bilgi Formu, ETKB-EVÇED-FRM-042 Rev.01, hesaplama dönemi 2023 (yayım 26.12.2025): Dağıtım hattından bağlı tüketim noktası, 0,469 tCO2-eşd./MWh.", "https://enerji.gov.tr/Media/Dizin/EVCED/tr/%C3%87evreVe%C4%B0klim/%C4%B0klimDe%C4%9Fi%C5%9Fikli%C4%9Fi/EmisyonFaktorleri/2023_Turkiye_Elektrik_UretimiveElektrik_Tuketim_Noktasi_Emisyon_Faktorleri.pdf", 2023 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ConversionFactors",
                keyColumn: "Id",
                keyValue: new Guid("77777777-7777-7777-7777-777777777777"));

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "ConversionFactors");

            migrationBuilder.DropColumn(
                name: "IsVerified",
                table: "ConversionFactors");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "ConversionFactors");

            migrationBuilder.DropColumn(
                name: "SourceUrl",
                table: "ConversionFactors");
        }
    }
}
