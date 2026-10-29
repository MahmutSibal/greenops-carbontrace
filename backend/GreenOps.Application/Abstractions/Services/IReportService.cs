using GreenOps.Application.Common;

namespace GreenOps.Application.Abstractions.Services;

public interface IReportService
{
    Task<Result<ReportGeneratedResponse>> GenerateIso14064Async(Guid companyId, int year, CancellationToken cancellationToken = default);
    Task<Result<ReportGeneratedResponse>> GenerateGhgProtocolAsync(Guid companyId, int year, CancellationToken cancellationToken = default);
    Task<Result<byte[]>> ExportExcelAsync(Guid companyId, string dataType, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<ReportHistoryDto>>> GetHistoryAsync(Guid companyId, CancellationToken cancellationToken = default);
}

public class ReportGeneratedResponse
{
    public Guid ReportId { get; set; }
    public string DownloadUrl { get; set; } = string.Empty;
    public string ReportType { get; set; } = string.Empty;
    public int Year { get; set; }
}

public class ReportHistoryDto
{
    public Guid Id { get; set; }
    public string ReportType { get; set; } = string.Empty;
    public int Year { get; set; }
    public string? FileUrl { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}
