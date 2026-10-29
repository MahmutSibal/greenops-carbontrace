using GreenOps.Application.Abstractions.Repositories;
using GreenOps.Application.Abstractions.Services;
using GreenOps.Application.Common;
using GreenOps.Domain.Entities;
using GreenOps.Infrastructure.Persistence;
using GreenOps.Infrastructure.Reports;
using Microsoft.EntityFrameworkCore;

namespace GreenOps.Infrastructure.Services;

public class ReportService : IReportService
{
    private readonly IReportRepository _reportRepository;
    private readonly AppDbContext _dbContext;

    public ReportService(IReportRepository reportRepository, AppDbContext dbContext)
    {
        _reportRepository = reportRepository;
        _dbContext = dbContext;
    }

    public async Task<Result<ReportGeneratedResponse>> GenerateIso14064Async(Guid companyId, int year, CancellationToken cancellationToken = default)
    {
        return await GenerateReportAsync(companyId, year, "ISO-14064", cancellationToken);
    }

    public async Task<Result<ReportGeneratedResponse>> GenerateGhgProtocolAsync(Guid companyId, int year, CancellationToken cancellationToken = default)
    {
        return await GenerateReportAsync(companyId, year, "GHG-Protocol", cancellationToken);
    }

    public async Task<Result<byte[]>> ExportExcelAsync(Guid companyId, string dataType, CancellationToken cancellationToken = default)
    {
        var entries = await _dbContext.EmissionEntries
            .AsNoTracking()
            .Include(e => e.Facility)
            .Include(e => e.Category)
            .Where(e => e.CompanyId == companyId && !e.IsDeleted)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);

        // Basit CSV export (production'da NPOI veya ClosedXML kullanılmalı)
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        var csv = new System.Text.StringBuilder();
        csv.AppendLine("Tesis,Kategori,Değer,Birim,Katsayı,Sonuç (kgCO2e),Dönem Başlangıcı,Dönem Bitişi,Giriş Tarihi,Durum");
        foreach (var entry in entries)
        {
            csv.AppendLine(string.Join(",",
                CsvField(entry.Facility?.Name),
                CsvField(entry.Category?.Name),
                entry.Value.ToString(inv),
                CsvField(entry.Unit ?? entry.Category?.Unit),
                entry.FactorValue?.ToString(inv) ?? "",
                entry.CalculationResult.ToString(inv),
                entry.PeriodStart?.ToString("yyyy-MM-dd") ?? "",
                entry.PeriodEnd?.ToString("yyyy-MM-dd") ?? "",
                entry.CreatedAt.ToString("yyyy-MM-dd"),
                entry.Status.ToString()));
        }

        // UTF-8 BOM ekle (Excel Türkçe karakter desteği için)
        var bom = new byte[] { 0xEF, 0xBB, 0xBF };
        var csvBytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
        var result = new byte[bom.Length + csvBytes.Length];
        bom.CopyTo(result, 0);
        csvBytes.CopyTo(result, bom.Length);
        return Result<byte[]>.Success(result);
    }

    /// <summary>Virgül/tırnak/satır sonunu kaçışlar; = + - @ ile başlayan değerleri formül olarak çalışmasın diye etkisizleştirir.</summary>
    private static string CsvField(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if ("=+-@\t\r".Contains(value[0])) value = "'" + value;
        return value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
    }

    public async Task<Result<ReportFile>> ExportInventoryAsync(Guid companyId, int year, string format, CancellationToken cancellationToken = default)
    {
        if (year < 1990 || year > DateTimeOffset.UtcNow.Year)
            return Result<ReportFile>.Failure("Geçersiz rapor yılı.");
        if (format is not ("pdf" or "xlsx"))
            return Result<ReportFile>.Failure("Biçim pdf veya xlsx olmalıdır.");

        var report = await InventoryReport.BuildAsync(_dbContext, companyId, year, cancellationToken);
        if (report is null)
            return Result<ReportFile>.Failure("Şirket bulunamadı.");

        var fileName = $"envanter_{year}_{DateTime.UtcNow:yyyyMMdd}.{format}";
        return Result<ReportFile>.Success(format == "pdf"
            ? new ReportFile(report.ToPdf(), "application/pdf", fileName)
            : new ReportFile(report.ToXlsx(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName));
    }

    public async Task<Result<IReadOnlyList<ReportHistoryDto>>> GetHistoryAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var reports = await _reportRepository.ListByCompanyAsync(companyId, cancellationToken);
        var dtos = reports.Select(r => new ReportHistoryDto
        {
            Id = r.Id,
            ReportType = r.ReportType,
            Year = r.Year,
            FileUrl = r.FileUrl,
            Status = r.Status,
            CreatedAt = r.CreatedAt
        }).ToList();

        return Result<IReadOnlyList<ReportHistoryDto>>.Success(dtos);
    }

    private Task<Result<ReportGeneratedResponse>> GenerateReportAsync(Guid companyId, int year, string reportType, CancellationToken cancellationToken)
    {
        if (year < 1990 || year > DateTimeOffset.UtcNow.Year)
            return Task.FromResult(Result<ReportGeneratedResponse>.Failure("Geçersiz rapor yılı."));

        // PDF üretimi yokken "Completed" durumlu, dosyasız rapor kaydı açmak denetimde yanıltıcıydı.
        // TODO: ISO 14064 / GHG Protocol PDF şablonu eklenince Report kaydı burada oluşturulmalı.
        return Task.FromResult(Result<ReportGeneratedResponse>.Failure(
            $"{reportType} PDF rapor üretimi henüz yapılandırılmadı. Veriyi export/excel ile alabilirsiniz."));
    }
}
