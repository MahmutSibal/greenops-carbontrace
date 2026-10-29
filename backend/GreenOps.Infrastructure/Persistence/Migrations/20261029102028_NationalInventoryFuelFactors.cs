using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace GreenOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class NationalInventoryFuelFactors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "ConversionFactors",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                columns: new[] { "IsActive", "Source" },
                values: new object[] { false, "Geliştirme yer tutucusu — resmi bir kaynağa dayanmaz (bkz. KAYNAKLAR.md bölüm 5.2). IPCC 2006 + Türkiye NID 2026 ısıl değeriyle değiştirildi." });

            migrationBuilder.UpdateData(
                table: "ConversionFactors",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                columns: new[] { "IsActive", "Source" },
                values: new object[] { false, "Geliştirme yer tutucusu — resmi bir kaynağa dayanmaz (bkz. KAYNAKLAR.md bölüm 5.2). IPCC 2006 + Türkiye NID 2026 ısıl değeriyle değiştirildi." });

            migrationBuilder.InsertData(
                table: "ConversionFactors",
                columns: new[] { "Id", "CategoryId", "Factor", "IsActive", "IsVerified", "Source", "SourceUrl", "Year" },
                values: new object[,]
                {
                    { new Guid("88888888-8888-8888-8888-888888888888"), new Guid("22222222-2222-2222-2222-222222222222"), 1.939576m, true, true, "Emisyon faktörü: 2006 IPCC Guidelines, Cilt 2, Bölüm 2, Tablo 2.3 (CO2 56.100, CH4 1, N2O 0,1 kg/TJ). Isıl değer: Türkiye Ulusal Sera Gazı Envanter Belgesi (NID 2026, 1990-2024), Ek \"NCV of Fuels\", 2024: 34,54 TJ/10^6 m3. GWP: IPCC AR5 (CH4 28, N2O 265).", "https://unfccc.int/sites/default/files/resource/TUR_NID_2026.pdf", 2024 },
                    { new Guid("99999999-9999-9999-9999-999999999999"), new Guid("33333333-3333-3333-3333-333333333333"), 2.673664m, true, true, "Emisyon faktörü: 2006 IPCC Guidelines, Cilt 2, Bölüm 2, Tablo 2.3 (CO2 74.100, CH4 3, N2O 0,6 kg/TJ). Isıl değer: Türkiye NID 2026, Ek \"NCV of Fuels\", 2024: motorin 43,33 TJ/kt. Yoğunluk: Enerji Kaynaklarının ve Enerjinin Kullanımında Verimliliğin Artırılmasına Dair Yönetmelik EK-2 (RG 27.10.2011/28097): 0,830 kg/L. GWP: IPCC AR5.", "https://unfccc.int/sites/default/files/resource/TUR_NID_2026.pdf", 2024 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ConversionFactors",
                keyColumn: "Id",
                keyValue: new Guid("88888888-8888-8888-8888-888888888888"));

            migrationBuilder.DeleteData(
                table: "ConversionFactors",
                keyColumn: "Id",
                keyValue: new Guid("99999999-9999-9999-9999-999999999999"));

            migrationBuilder.UpdateData(
                table: "ConversionFactors",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                columns: new[] { "IsActive", "Source" },
                values: new object[] { true, "Geliştirme yer tutucusu — resmi bir kaynağa dayanmaz (bkz. KAYNAKLAR.md bölüm 5.2)." });

            migrationBuilder.UpdateData(
                table: "ConversionFactors",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                columns: new[] { "IsActive", "Source" },
                values: new object[] { true, "Geliştirme yer tutucusu — resmi bir kaynağa dayanmaz (bkz. KAYNAKLAR.md bölüm 5.2)." });
        }
    }
}
