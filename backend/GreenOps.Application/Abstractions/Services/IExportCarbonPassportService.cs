using GreenOps.Application.Common;

namespace GreenOps.Application.Abstractions.Services;

public interface IExportCarbonPassportService
{
    Task<Result<ExportCarbonPassportResponse>> CreateAsync(
        Guid companyId,
        CreateExportCarbonPassportRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<ExportCarbonPassportResponse>> GetAsync(
        Guid companyId,
        Guid passportId,
        CancellationToken cancellationToken = default);

    Task<Result<PublicExportCarbonPassportResponse>> GetPublicAsync(
        string publicToken,
        CancellationToken cancellationToken = default);

    Task<Result<ReportFile>> ExportPdfAsync(
        Guid companyId,
        Guid passportId,
        CancellationToken cancellationToken = default);

    Task<Result> RevokeAsync(
        Guid companyId,
        Guid passportId,
        CancellationToken cancellationToken = default);
}

public class CreateExportCarbonPassportRequest
{
    public string ProductName { get; set; } = string.Empty;
    public string? ProductCode { get; set; }
    public string? ShipmentReference { get; set; }
    public decimal Quantity { get; set; }
    public string QuantityUnit { get; set; } = "ton";
    public DateTimeOffset ProductionStart { get; set; }
    public DateTimeOffset ProductionEnd { get; set; }
    public List<Guid>? EmissionEntryIds { get; set; }
}

public class ExportCarbonPassportResponse
{
    public Guid Id { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductCode { get; set; }
    public string? ShipmentReference { get; set; }
    public decimal Quantity { get; set; }
    public string QuantityUnit { get; set; } = string.Empty;
    public DateTimeOffset ProductionStart { get; set; }
    public DateTimeOffset ProductionEnd { get; set; }
    public decimal TotalEmissionsTonnes { get; set; }
    public decimal EmissionsPerUnitTonnes { get; set; }
    public decimal DataReliabilityPercent { get; set; }
    public int EvidenceCount { get; set; }
    public int EmissionEntryCount { get; set; }
    public string PublicUrl { get; set; } = string.Empty;
    public string? PublicToken { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class PublicExportCarbonPassportResponse
{
    public string PassportId { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? ProductCode { get; set; }
    public string? ShipmentReference { get; set; }
    public decimal Quantity { get; set; }
    public string QuantityUnit { get; set; } = string.Empty;
    public DateTimeOffset ProductionStart { get; set; }
    public DateTimeOffset ProductionEnd { get; set; }
    public decimal EmissionsTonnes { get; set; }
    public decimal EmissionsPerUnitTonnes { get; set; }
    public decimal DataReliabilityPercent { get; set; }
    public int EvidenceCount { get; set; }
    public int EmissionEntryCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string VerificationMessage { get; set; } = "CarbonTrace tarafından oluşturulan doğrulama özeti.";
}
