using System.Globalization;
using ClosedXML.Excel;
using GreenOps.Domain.Entities;
using GreenOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GreenOps.Infrastructure.Reports;

/// <summary>
/// Yıllık sera gazı envanteri: yalnızca onaylı kayıtlar, her kalemin katsayısı ve kaynağıyla.
/// PDF özet + Excel (özet ve kayıt kayıt döküm) aynı veri modelinden üretilir.
/// Rapor üçüncü taraf doğrulaması iddia etmez; doğrulanmamış katsayıyla hesaplanan pay açıkça gösterilir.
/// </summary>
public sealed class InventoryReport
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    public string CompanyName { get; private init; } = string.Empty;
    public int Year { get; private init; }
    public DateTimeOffset GeneratedAt { get; private init; }
    public int ApprovedCount { get; private init; }
    public int PendingCount { get; private init; }
    public int RejectedCount { get; private init; }
    public decimal Total { get; private init; }
    public decimal VerifiedFactorShare { get; private init; }
    public IReadOnlyList<(int Scope, decimal Emissions)> ByScope { get; private init; } = [];
    public IReadOnlyList<CategoryRow> ByCategory { get; private init; } = [];
    public IReadOnlyList<(string Facility, decimal Emissions)> ByFacility { get; private init; } = [];
    public IReadOnlyList<(int Month, decimal Emissions)> ByMonth { get; private init; } = [];
    public IReadOnlyList<EntryRow> Entries { get; private init; } = [];

    public sealed record CategoryRow(string Category, int? Scope, decimal Consumption, string Unit, decimal Emissions, string Factors, bool AllVerified);

    public sealed record EntryRow(
        DateTimeOffset Date, string Facility, string Category, int? Scope, decimal Value, string Unit,
        decimal? Factor, int? FactorYear, string? FactorSource, bool FactorVerified, decimal Emissions,
        string? CreatedBy, string? ApprovedBy, DateTimeOffset? ApprovedAt);

    public static async Task<InventoryReport?> BuildAsync(AppDbContext db, Guid companyId, int year, CancellationToken ct)
    {
        var companyName = await db.Companies.AsNoTracking()
            .Where(c => c.Id == companyId).Select(c => c.Name).FirstOrDefaultAsync(ct);
        if (companyName is null) return null;

        var start = new DateTimeOffset(year, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var end = start.AddYears(1);
        var inYear = db.EmissionEntries.AsNoTracking()
            .Where(e => e.CompanyId == companyId && !e.IsDeleted
                        && (e.PeriodStart ?? e.CreatedAt) >= start && (e.PeriodStart ?? e.CreatedAt) < end);

        var statusCounts = await inYear.GroupBy(e => e.Status)
            .Select(g => new { g.Key, Count = g.Count() }).ToListAsync(ct);
        int Count(EmissionEntryStatus s) => statusCounts.FirstOrDefault(x => x.Key == s)?.Count ?? 0;

        var entries = await inYear.Where(e => e.Status == EmissionEntryStatus.Approved)
            .OrderBy(e => e.PeriodStart ?? e.CreatedAt)
            .Select(e => new EntryRow(
                e.PeriodStart ?? e.CreatedAt,
                e.Facility!.Name,
                e.Category!.Name,
                (int?)e.Category!.Scope!.Number,
                e.Value,
                e.Unit ?? e.Category!.Unit,
                e.FactorValue,
                (int?)e.ConversionFactor!.Year,
                e.ConversionFactor!.Source,
                e.ConversionFactor != null && e.ConversionFactor.IsVerified,
                e.CalculationResult,
                e.CreatedByUser!.FullName,
                e.ApprovedByUser!.FullName,
                e.ApprovedAt))
            .ToListAsync(ct);

        var total = entries.Sum(e => e.Emissions);
        return new InventoryReport
        {
            CompanyName = companyName,
            Year = year,
            GeneratedAt = DateTimeOffset.UtcNow,
            ApprovedCount = entries.Count,
            PendingCount = Count(EmissionEntryStatus.Pending),
            RejectedCount = Count(EmissionEntryStatus.Rejected),
            Total = total,
            VerifiedFactorShare = total > 0 ? entries.Where(e => e.FactorVerified).Sum(e => e.Emissions) / total * 100 : 0,
            ByScope = entries.GroupBy(e => e.Scope ?? 0).OrderBy(g => g.Key)
                .Select(g => (g.Key, g.Sum(x => x.Emissions))).ToList(),
            ByCategory = entries.GroupBy(e => e.Category)
                .Select(g => new CategoryRow(
                    CategoryName(g.Key), g.First().Scope, g.Sum(x => x.Value), g.First().Unit, g.Sum(x => x.Emissions),
                    string.Join("; ", g.Select(x => x.Factor).Distinct().Select(f => f?.ToString("0.######", Tr) ?? "—")),
                    g.All(x => x.FactorVerified)))
                .OrderByDescending(c => c.Emissions).ToList(),
            ByFacility = entries.GroupBy(e => e.Facility)
                .Select(g => (g.Key, g.Sum(x => x.Emissions))).OrderByDescending(x => x.Item2).ToList(),
            ByMonth = entries.GroupBy(e => e.Date.Month).OrderBy(g => g.Key)
                .Select(g => (g.Key, g.Sum(x => x.Emissions))).ToList(),
            Entries = entries
        };
    }

    // ───────────────────────────── PDF ─────────────────────────────

    public byte[] ToPdf()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var green = Color.FromHex("#1B7F5A");

        return QuestPDF.Fluent.Document.Create(doc => doc.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(36);
            page.DefaultTextStyle(t => t.FontSize(9.5f).FontFamily("Arial"));

            page.Header().Column(col =>
            {
                col.Item().Text("Sera Gazı Envanter Özeti").FontSize(18).Bold().FontColor(green);
                col.Item().Text($"{CompanyName} · {Year} takvim yılı").FontSize(11);
                col.Item().PaddingTop(2).Text($"Oluşturulma: {GeneratedAt.ToOffset(TimeSpan.FromHours(3)):dd.MM.yyyy HH:mm} (TSİ) · GreenOps CarbonTrace")
                    .FontSize(8).FontColor(Colors.Grey.Darken1);
                col.Item().PaddingTop(6).LineHorizontal(1).LineColor(green);
            });

            page.Content().PaddingTop(10).Column(col =>
            {
                col.Spacing(12);

                col.Item().Row(row =>
                {
                    Kpi(row.RelativeItem(), "Toplam emisyon", $"{Tonnes(Total)} tCO₂e");
                    Kpi(row.RelativeItem(), "Onaylı kayıt", ApprovedCount.ToString(Tr));
                    Kpi(row.RelativeItem(), "Doğrulanmış katsayı payı", $"%{VerifiedFactorShare.ToString("0.#", Tr)}");
                });

                col.Item().Element(Section("Kapsama göre")).Table(t =>
                {
                    t.ColumnsDefinition(c => { c.RelativeColumn(3); c.RelativeColumn(2); c.RelativeColumn(1); });
                    HeaderRow(t, "Kapsam", "tCO₂e", "Pay");
                    foreach (var (scope, e) in ByScope)
                        Row(t, ScopeName(scope), Tonnes(e), Share(e));
                });

                col.Item().Element(Section("Kategoriye göre (faaliyet verisi ve katsayı)")).Table(t =>
                {
                    t.ColumnsDefinition(c => { c.RelativeColumn(2); c.RelativeColumn(1); c.RelativeColumn(2); c.RelativeColumn(2); c.RelativeColumn(2); });
                    HeaderRow(t, "Kategori", "Kapsam", "Tüketim", "Katsayı (kgCO₂e/birim)", "tCO₂e");
                    foreach (var c in ByCategory)
                        Row(t, c.Category, c.Scope?.ToString() ?? "—", $"{c.Consumption.ToString("N0", Tr)} {c.Unit}",
                            c.AllVerified ? c.Factors : c.Factors + " (örnek katsayı)", Tonnes(c.Emissions));
                });

                col.Item().Row(row =>
                {
                    row.RelativeItem().Element(Section("Tesise göre")).Table(t =>
                    {
                        t.ColumnsDefinition(c => { c.RelativeColumn(3); c.RelativeColumn(2); });
                        HeaderRow(t, "Tesis", "tCO₂e");
                        foreach (var (f, e) in ByFacility) Row(t, f, Tonnes(e));
                    });
                    row.ConstantItem(16);
                    row.RelativeItem().Element(Section("Aya göre")).Table(t =>
                    {
                        t.ColumnsDefinition(c => { c.RelativeColumn(3); c.RelativeColumn(2); });
                        HeaderRow(t, "Ay", "tCO₂e");
                        foreach (var (m, e) in ByMonth) Row(t, Tr.DateTimeFormat.GetMonthName(m), Tonnes(e));
                    });
                });

                col.Item().Element(Section("Yöntem ve sınırlar")).Column(n =>
                {
                    n.Spacing(3);
                    n.Item().Text("• Yalnızca onaylanmış kayıtlar dahildir. Bu yıla ait " +
                                  $"{PendingCount} onay bekleyen ve {RejectedCount} reddedilen kayıt hesaba katılmamıştır.");
                    n.Item().Text("• Emisyon = faaliyet verisi × emisyon katsayısı. Her kayıt, hesaplandığı andaki katsayıyı saklar; " +
                                  "katsayı sonradan güncellense de geçmiş kayıtlar değişmez.");
                    if (ByCategory.Any(c => c.Category == "Elektrik"))
                        n.Item().Text("• Kapsam 2 elektrik emisyonu konum bazlı (location-based) hesaplanmıştır: T.C. Enerji ve Tabii Kaynaklar " +
                                      "Bakanlığı, 2023 dönemi Türkiye elektrik tüketim noktası emisyon faktörü.");
                    if (ByCategory.Any(c => !c.AllVerified))
                        n.Item().Text("• \"Örnek katsayı\" ile işaretli kategoriler henüz resmi kaynağa bağlanmamış yer tutucu değerle hesaplanmıştır; " +
                                      "bu kalemler bilgilendirme amaçlıdır.");
                    n.Item().Text("• Bu belge kurum içi bir envanter özetidir; ISO 14064-3 kapsamında üçüncü taraf doğrulaması yapılmamıştır.");
                });
            });

            page.Footer().AlignRight().Text(t =>
            {
                t.DefaultTextStyle(s => s.FontSize(8).FontColor(Colors.Grey.Darken1));
                t.Span("Sayfa "); t.CurrentPageNumber(); t.Span(" / "); t.TotalPages();
            });
        })).GeneratePdf();

        static void Kpi(IContainer c, string label, string value) =>
            c.Border(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(8).Column(k =>
            {
                k.Item().Text(label).FontSize(8).FontColor(Colors.Grey.Darken1);
                k.Item().Text(value).FontSize(14).Bold();
            });

        // Başlık + içerik: bir container tek çocuk alır, bu yüzden Decoration ile başlık üste yerleştirilir.
        static Func<IContainer, IContainer> Section(string title) => c =>
        {
            IContainer content = null!;
            c.Decoration(d =>
            {
                d.Before().PaddingBottom(4).Text(title).FontSize(11).SemiBold();
                content = d.Content();
            });
            return content;
        };
    }

    private static void HeaderRow(TableDescriptor t, params string[] cells)
    {
        foreach (var cell in cells)
            t.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text(cell).SemiBold();
    }

    private static void Row(TableDescriptor t, params string[] cells)
    {
        foreach (var cell in cells)
            t.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(cell);
    }

    // ───────────────────────────── Excel ─────────────────────────────

    public byte[] ToXlsx()
    {
        using var wb = new XLWorkbook();

        var s = wb.AddWorksheet("Özet");
        s.Cell(1, 1).Value = $"Sera Gazı Envanter Özeti — {CompanyName} — {Year}";
        s.Cell(1, 1).Style.Font.Bold = true;
        s.Cell(1, 1).Style.Font.FontSize = 14;
        s.Cell(2, 1).Value = $"Oluşturulma: {GeneratedAt.ToOffset(TimeSpan.FromHours(3)):dd.MM.yyyy HH:mm} (TSİ). Yalnızca onaylı kayıtlar; üçüncü taraf doğrulaması yapılmamıştır.";

        var r = 4;
        r = Table(s, r, "Kapsama göre", new[] { "Kapsam", "kgCO2e", "tCO2e" },
            ByScope.Select(x => new XLCellValue[] { ScopeName(x.Scope), x.Emissions, Math.Round(x.Emissions / 1000, 3) }));
        r = Table(s, r, "Kategoriye göre", new[] { "Kategori", "Kapsam", "Tüketim", "Birim", "Katsayı", "Katsayı doğrulanmış mı", "kgCO2e" },
            ByCategory.Select(c => new XLCellValue[] { c.Category, c.Scope?.ToString() ?? "", c.Consumption, c.Unit, c.Factors, c.AllVerified ? "Evet" : "Hayır (örnek katsayı)", c.Emissions }));
        r = Table(s, r, "Tesise göre", new[] { "Tesis", "kgCO2e" },
            ByFacility.Select(f => new XLCellValue[] { f.Facility, f.Emissions }));
        Table(s, r, "Aya göre", new[] { "Ay", "kgCO2e" },
            ByMonth.Select(m => new XLCellValue[] { Tr.DateTimeFormat.GetMonthName(m.Month), m.Emissions }));
        s.Columns().AdjustToContents(8, 60);

        var d = wb.AddWorksheet("Kayıtlar");
        var headers = new[] { "Dönem", "Tesis", "Kategori", "Kapsam", "Tüketim", "Birim", "Katsayı", "Katsayı yılı", "Katsayı kaynağı", "Doğrulanmış", "kgCO2e", "Giren", "Onaylayan", "Onay tarihi" };
        for (var i = 0; i < headers.Length; i++) d.Cell(1, i + 1).Value = headers[i];
        d.Row(1).Style.Font.Bold = true;
        var row = 2;
        foreach (var e in Entries)
        {
            XLCellValue[] values =
            {
                e.Date.ToString("yyyy-MM"), Safe(e.Facility), CategoryName(e.Category), e.Scope?.ToString() ?? "",
                e.Value, e.Unit, e.Factor is { } f ? f : "", e.FactorYear is { } y ? y : "", Safe(e.FactorSource),
                e.FactorVerified ? "Evet" : "Hayır", e.Emissions, Safe(e.CreatedBy), Safe(e.ApprovedBy),
                e.ApprovedAt?.ToString("yyyy-MM-dd") ?? ""
            };
            for (var i = 0; i < values.Length; i++) d.Cell(row, i + 1).Value = values[i];
            row++;
        }
        d.SheetView.FreezeRows(1);
        d.Columns().AdjustToContents(8, 60);

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private static int Table(IXLWorksheet s, int row, string title, string[] headers, IEnumerable<XLCellValue[]> rows)
    {
        s.Cell(row, 1).Value = title;
        s.Cell(row, 1).Style.Font.Bold = true;
        row++;
        for (var i = 0; i < headers.Length; i++)
        {
            s.Cell(row, i + 1).Value = headers[i];
            s.Cell(row, i + 1).Style.Font.Bold = true;
            s.Cell(row, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#E8F3EE");
        }
        row++;
        foreach (var values in rows)
        {
            for (var i = 0; i < values.Length; i++)
                s.Cell(row, i + 1).Value = values[i].IsText ? Safe(values[i].GetText()) : values[i];
            row++;
        }
        return row + 1;
    }

    // ───────────────────────────── Yardımcılar ─────────────────────────────

    /// <summary>Kullanıcı girişli metin (tesis adı vb.) Excel'de formül olarak yorumlanmasın.</summary>
    private static string Safe(string? value) =>
        string.IsNullOrEmpty(value) ? "" : "=+-@".Contains(value[0]) ? "'" + value : value;

    private static string CategoryName(string name) => name switch
    {
        "Electricity" => "Elektrik",
        "Natural Gas" => "Doğalgaz",
        "Diesel" => "Motorin",
        _ => name
    };

    private static string ScopeName(int scope) => scope switch
    {
        1 => "Kapsam 1 (doğrudan)",
        2 => "Kapsam 2 (satın alınan enerji)",
        3 => "Kapsam 3 (diğer dolaylı)",
        _ => "Kapsam belirtilmemiş"
    };

    private string Share(decimal e) => Total > 0 ? $"%{(e / Total * 100).ToString("0.#", Tr)}" : "—";

    private static string Tonnes(decimal kg) => (kg / 1000m).ToString("N1", Tr);
}
