using GreenOps.Application.Common;

namespace GreenOps.Application.Abstractions.Services;

public interface ITargetService
{
    Task<Result<IReadOnlyList<TargetDto>>> ListByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<Result<Guid>> CreateTargetAsync(CreateTargetRequest request, CancellationToken cancellationToken = default);
    Task<Result<TargetProgressDto>> GetProgressAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<Result> PurchaseCarbonCreditAsync(PurchaseCreditRequest request, CancellationToken cancellationToken = default);
}

public class TargetDto
{
    public Guid Id { get; set; }
    public int TargetYear { get; set; }
    public decimal ReductionPercent { get; set; }
    public int BaselineYear { get; set; }
    public decimal BaselineEmissions { get; set; }
    public string? Description { get; set; }
}

public class CreateTargetRequest
{
    public Guid CompanyId { get; set; }
    public int TargetYear { get; set; }
    public decimal ReductionPercent { get; set; }
    public int BaselineYear { get; set; }
    public decimal BaselineEmissions { get; set; }
    public string? Description { get; set; }
}

public class TargetProgressDto
{
    public decimal BaselineEmissions { get; set; }
    /// <summary>Bu yıl şimdiye kadar onaylanmış emisyon (kgCO2e).</summary>
    public decimal CurrentEmissions { get; set; }
    /// <summary>Yıl tamamlanmadıysa, veri olan ayların ortalamasıyla 12 aya ölçeklenmiş tahmin; tamamlandıysa gerçek toplam.</summary>
    public decimal ProjectedAnnualEmissions { get; set; }
    /// <summary>Bu yıl onaylı verisi olan ay sayısı (0-12).</summary>
    public int MonthsCovered { get; set; }
    public bool IsProjected { get; set; }
    public decimal TargetEmissions { get; set; }
    /// <summary>Yıllık (tahmini) emisyona göre hedefe ilerleme; veri yoksa 0.</summary>
    public decimal ProgressPercent { get; set; }
    public decimal CarbonCreditsOffset { get; set; }
}

public class PurchaseCreditRequest
{
    public Guid CompanyId { get; set; }
    public decimal Credits { get; set; }
    public string? CertificateUrl { get; set; }
    public string? Provider { get; set; }
}
