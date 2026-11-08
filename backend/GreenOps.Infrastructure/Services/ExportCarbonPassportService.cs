using GreenOps.Application.Abstractions.Services;
using GreenOps.Application.Common;
using GreenOps.Domain.Entities;
using GreenOps.Infrastructure.Persistence;
using GreenOps.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestPdfDocument = QuestPDF.Fluent.Document;

namespace GreenOps.Infrastructure.Services;

public class ExportCarbonPassportService : IExportCarbonPassportService
{
    private readonly AppDbContext _dbContext;

    public ExportCarbonPassportService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<ExportCarbonPassportResponse>> CreateAsync(
        Guid companyId,
        CreateExportCarbonPassportRequest request,
        CancellationToken cancellationToken = default)
    {
        var productName = request.ProductName?.Trim() ?? string.Empty;
        if (productName.Length is < 2 or > 200)
            return Result<ExportCarbonPassportResponse>.Failure("Ürün adı 2-200 karakter olmalıdır.");
        if (request.Quantity <= 0 || request.Quantity > 1_000_000_000)
            return Result<ExportCarbonPassportResponse>.Failure("Ürün miktarı 0'dan büyük olmalıdır.");
        if (request.ProductionEnd < request.ProductionStart)
            return Result<ExportCarbonPassportResponse>.Failure("Üretim bitişi başlangıçtan önce olamaz.");
        if (request.ProductionEnd - request.ProductionStart > TimeSpan.FromDays(366))
            return Result<ExportCarbonPassportResponse>.Failure("Üretim dönemi en fazla 366 gün olabilir.");

        var unit = NormalizeUnit(request.QuantityUnit);
        if (unit is null)
            return Result<ExportCarbonPassportResponse>.Failure("Miktar birimi ton olmalıdır.");

        var query = _dbContext.EmissionEntries
            .AsNoTracking()
            .Include(e => e.Documents)
            .Include(e => e.ConversionFactor)
            .Where(e => e.CompanyId == companyId
                        && !e.IsDeleted
                        && e.Status == EmissionEntryStatus.Approved);

        if (request.EmissionEntryIds is { Count: > 0 })
        {
            var ids = request.EmissionEntryIds.Distinct().ToList();
            query = query.Where(e => ids.Contains(e.Id));
        }
        else
        {
            query = query.Where(e =>
                (e.PeriodStart ?? e.CreatedAt) >= request.ProductionStart
                && (e.PeriodStart ?? e.CreatedAt) <= request.ProductionEnd);
        }

        var entries = await query.ToListAsync(cancellationToken);
        if (entries.Count == 0)
            return Result<ExportCarbonPassportResponse>.Failure("Pasaporta bağlanacak onaylı emisyon kaydı bulunamadı.");

        if (request.EmissionEntryIds is { Count: > 0 }
            && entries.Count != request.EmissionEntryIds.Distinct().Count())
            return Result<ExportCarbonPassportResponse>.Failure("Seçilen kayıtların bir kısmı şirkete ait değil veya onaylanmamış.");

        var totalKg = entries.Sum(e => e.CalculationResult);
        var reliability = entries.Average(ReliabilityScore);
        var token = TokenHasher.NewUrlSafeToken(32);
        var passport = new ExportCarbonPassport
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            ProductName = productName,
            ProductCode = TrimOptional(request.ProductCode, 100),
            ShipmentReference = TrimOptional(request.ShipmentReference, 100),
            Quantity = request.Quantity,
            QuantityUnit = unit,
            ProductionStart = request.ProductionStart,
            ProductionEnd = request.ProductionEnd,
            TotalEmissionsKg = totalKg,
            EmissionsPerUnitKg = totalKg / request.Quantity,
            DataReliabilityPercent = decimal.Round(reliability, 1),
            PublicTokenHash = TokenHasher.Sha256(token),
            CreatedAt = DateTimeOffset.UtcNow
        };

        foreach (var entry in entries)
            passport.Entries.Add(new ExportCarbonPassportEntry { PassportId = passport.Id, EmissionEntryId = entry.Id });

        await _dbContext.ExportCarbonPassports.AddAsync(passport, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<ExportCarbonPassportResponse>.Success(ToResponse(passport, entries, token));
    }

    public async Task<Result<ExportCarbonPassportResponse>> GetAsync(
        Guid companyId,
        Guid passportId,
        CancellationToken cancellationToken = default)
    {
        var passport = await QueryPassports()
            .FirstOrDefaultAsync(p => p.Id == passportId && p.CompanyId == companyId, cancellationToken);
        if (passport is null)
            return Result<ExportCarbonPassportResponse>.Failure("Karbon pasaportu bulunamadı.");

        return Result<ExportCarbonPassportResponse>.Success(ToResponse(passport, passport.Entries.Select(x => x.EmissionEntry!).ToList()));
    }

    public async Task<Result<PublicExportCarbonPassportResponse>> GetPublicAsync(
        string publicToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(publicToken))
            return Result<PublicExportCarbonPassportResponse>.Failure("Geçersiz doğrulama bağlantısı.");

        var passport = await QueryPassports()
            .FirstOrDefaultAsync(p => p.PublicTokenHash == TokenHasher.Sha256(publicToken)
                                      && p.RevokedAt == null
                                      && p.Company!.IsActive, cancellationToken);
        if (passport is null)
            return Result<PublicExportCarbonPassportResponse>.Failure("Karbon pasaportu bulunamadı veya iptal edildi.");

        return Result<PublicExportCarbonPassportResponse>.Success(new PublicExportCarbonPassportResponse
        {
            PassportId = passport.Id.ToString(),
            CompanyName = passport.Company!.Name,
            ProductName = passport.ProductName,
            ProductCode = passport.ProductCode,
            ShipmentReference = passport.ShipmentReference,
            Quantity = passport.Quantity,
            QuantityUnit = passport.QuantityUnit,
            ProductionStart = passport.ProductionStart,
            ProductionEnd = passport.ProductionEnd,
            EmissionsTonnes = Tonnes(passport.TotalEmissionsKg),
            EmissionsPerUnitTonnes = Tonnes(passport.EmissionsPerUnitKg),
            DataReliabilityPercent = passport.DataReliabilityPercent,
            EvidenceCount = passport.Entries.SelectMany(x => x.EmissionEntry!.Documents).Count()
                            + passport.Entries.Count(x => !string.IsNullOrWhiteSpace(x.EmissionEntry!.EvidenceUrl)),
            EmissionEntryCount = passport.Entries.Count,
            CreatedAt = passport.CreatedAt
        });
    }

    public async Task<Result<ReportFile>> ExportPdfAsync(
        Guid companyId,
        Guid passportId,
        CancellationToken cancellationToken = default)
    {
        var passport = await QueryPassports()
            .FirstOrDefaultAsync(p => p.Id == passportId && p.CompanyId == companyId, cancellationToken);
        if (passport is null)
            return Result<ReportFile>.Failure("Karbon pasaportu bulunamadı.");

        QuestPDF.Settings.License = LicenseType.Community;
        var pdf = QuestPdfDocument.Create(document => document.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(42);
            page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));
            page.Header().Column(column =>
            {
                column.Item().Text("CARBONTRACE EXPORT PASSPORT").FontSize(20).Bold().FontColor(Color.FromHex("#1B7F5A"));
                column.Item().Text($"{passport.Company!.Name} · {passport.ProductName}").FontSize(12);
                column.Item().Text($"Oluşturulma: {passport.CreatedAt:dd.MM.yyyy HH:mm} UTC").FontSize(8).FontColor(Colors.Grey.Darken1);
            });
            page.Content().PaddingTop(18).Column(column =>
            {
                column.Spacing(10);
                column.Item().Text("Ürün ve sevkiyat bilgileri").FontSize(13).SemiBold();
                InfoTable(column, "Ürün", passport.ProductName);
                InfoTable(column, "Ürün kodu", passport.ProductCode ?? "—");
                InfoTable(column, "Sevkiyat referansı", passport.ShipmentReference ?? "—");
                InfoTable(column, "Miktar", $"{passport.Quantity:N2} {passport.QuantityUnit}");
                InfoTable(column, "Üretim dönemi", $"{passport.ProductionStart:dd.MM.yyyy} - {passport.ProductionEnd:dd.MM.yyyy}");
                column.Item().PaddingTop(8).Text("Karbon özeti").FontSize(13).SemiBold();
                InfoTable(column, "Toplam emisyon", $"{Tonnes(passport.TotalEmissionsKg):N3} tCO₂e");
                InfoTable(column, "Birim emisyon", $"{Tonnes(passport.EmissionsPerUnitKg):N3} tCO₂e/{passport.QuantityUnit}");
                InfoTable(column, "Veri güvenilirliği", $"%{passport.DataReliabilityPercent:N1}");
                InfoTable(column, "Kanıt sayısı", EvidenceCount(passport).ToString());
                InfoTable(column, "Bağlı onaylı kayıt", passport.Entries.Count.ToString());
                column.Item().PaddingTop(8).Background(Color.FromHex("#E8F5EF")).Padding(10)
                    .Text("Bu belge, CarbonTrace üzerinde onaylanmış emisyon kayıtlarından üretilen bir doğrulama özetidir. "
                          + "Üçüncü taraf doğrulama sertifikası değildir.");
            });
            page.Footer().AlignCenter().Text("GreenOps CarbonTrace · Karbonu kanıtıyla birlikte taşıyın");
        })).GeneratePdf();

        return Result<ReportFile>.Success(new ReportFile(
            pdf,
            "application/pdf",
            $"carbontrace-passport-{passport.Id:N}.pdf"));
    }

    public async Task<Result> RevokeAsync(
        Guid companyId,
        Guid passportId,
        CancellationToken cancellationToken = default)
    {
        var passport = await _dbContext.ExportCarbonPassports
            .FirstOrDefaultAsync(p => p.Id == passportId && p.CompanyId == companyId, cancellationToken);
        if (passport is null)
            return Result.Failure("Karbon pasaportu bulunamadı.");

        passport.RevokedAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private IQueryable<ExportCarbonPassport> QueryPassports() =>
        _dbContext.ExportCarbonPassports
            .Include(p => p.Company)
            .Include(p => p.Entries)
                .ThenInclude(x => x.EmissionEntry!)
                    .ThenInclude(e => e.Documents);

    private static ExportCarbonPassportResponse ToResponse(
        ExportCarbonPassport passport,
        IReadOnlyCollection<EmissionEntry> entries,
        string? token = null) =>
        new()
        {
            Id = passport.Id,
            ProductName = passport.ProductName,
            ProductCode = passport.ProductCode,
            ShipmentReference = passport.ShipmentReference,
            Quantity = passport.Quantity,
            QuantityUnit = passport.QuantityUnit,
            ProductionStart = passport.ProductionStart,
            ProductionEnd = passport.ProductionEnd,
            TotalEmissionsTonnes = Tonnes(passport.TotalEmissionsKg),
            EmissionsPerUnitTonnes = Tonnes(passport.EmissionsPerUnitKg),
            DataReliabilityPercent = passport.DataReliabilityPercent,
            EvidenceCount = EvidenceCount(passport),
            EmissionEntryCount = entries.Count,
            PublicToken = token,
            PublicUrl = $"/api/public/carbon-passports/{token ?? "token-hidden"}",
            CreatedAt = passport.CreatedAt
        };

    private static decimal ReliabilityScore(EmissionEntry entry)
    {
        var score = 40m;
        if (entry.ConversionFactor?.IsVerified == true) score += 40;
        if (!string.IsNullOrWhiteSpace(entry.EvidenceUrl) || entry.Documents.Count > 0) score += 10;
        if (entry.PeriodStart.HasValue && entry.PeriodEnd.HasValue) score += 10;
        return score;
    }

    private static int EvidenceCount(ExportCarbonPassport passport) =>
        passport.Entries.SelectMany(x => x.EmissionEntry!.Documents).Count()
        + passport.Entries.Count(x => !string.IsNullOrWhiteSpace(x.EmissionEntry!.EvidenceUrl));

    private static string? NormalizeUnit(string? unit) =>
        unit?.Trim().ToLowerInvariant() switch
        {
            "ton" or "tonne" or "tonnes" or "t" => "ton",
            _ => null
        };

    private static string? TrimOptional(string? value, int maxLength)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed[..Math.Min(trimmed.Length, maxLength)];
    }

    private static decimal Tonnes(decimal kilograms) => kilograms / 1000m;

    private static void InfoTable(ColumnDescriptor column, string label, string value) =>
        column.Item().Row(row =>
        {
            row.ConstantItem(150).Text(label).SemiBold();
            row.RelativeItem().Text(value);
        });
}
