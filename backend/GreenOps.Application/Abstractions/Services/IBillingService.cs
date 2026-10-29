using GreenOps.Application.Common;

namespace GreenOps.Application.Abstractions.Services;

public interface IBillingService
{
    Task<Result<BillingPlanDto>> GetCurrentPlanAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<Result> UpgradePlanAsync(Guid companyId, UpgradePlanRequest request, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<InvoiceDto>>> ListInvoicesAsync(Guid companyId, CancellationToken cancellationToken = default);
}

public class BillingPlanDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal MonthlyPrice { get; set; }
    public int MaxFacilities { get; set; }
    public int MaxUsersCount { get; set; }
    public bool HasApiAccess { get; set; }
    public bool HasIotIntegration { get; set; }
    public bool HasAiPrediction { get; set; }
}

public class UpgradePlanRequest
{
    public Guid NewPlanId { get; set; }
    public string? PaymentToken { get; set; }   // Stripe/Iyzico payment token
}

public class InvoiceDto
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "TRY";
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset InvoiceDate { get; set; }
    public string? PdfUrl { get; set; }
}
