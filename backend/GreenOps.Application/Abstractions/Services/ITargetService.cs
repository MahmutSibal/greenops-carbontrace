using GreenOps.Application.Common;

namespace GreenOps.Application.Abstractions.Services;

public interface ITargetService
{
    Task<Result<IReadOnlyList<TargetDto>>> ListByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<Result<Guid>> CreateTargetAsync(CreateTargetRequest request, CancellationToken cancellationToken = default);
    Task<Result<TargetProgressDto>> GetProgressAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<Result> PurchaseCarbonCreditAsync(PurchaseCreditRequest request, CancellationToken cancellationToken = default);
    Task<Result<ScenarioResultDto>> SimulateScenarioAsync(ScenarioRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// "Bu kategoride tüketimi %X azaltırsak" senaryosu. Önlem başına sabit tasarruf oranı uydurulmaz;
/// yüzdeyi kullanıcı (ör. enerji etüdü veya tedarikçi teklifine dayanarak) girer, sistem yalnızca hesaplar.
/// </summary>
public class ScenarioRequest
{
    public Guid CompanyId { get; set; }
    public List<ScenarioMeasure> Measures { get; set; } = new();
}

public class ScenarioMeasure
{
    public Guid CategoryId { get; set; }
    /// <summary>Kategorideki tüketimin azalma yüzdesi (0-100).</summary>
    public decimal ReductionPercent { get; set; }
    public string? Label { get; set; }
}

public class ScenarioResultDto
{
    /// <summary>Hesap penceresi: son 12 tam ay (bu ay hariç).</summary>
    public DateTimeOffset WindowStart { get; set; }
    public DateTimeOffset WindowEnd { get; set; }
    public int MonthsWithData { get; set; }
    /// <summary>true ise 12 aydan az veri vardır; tüm tutarlar veri olan ayların ortalamasıyla yıllığa ölçeklenmiştir.</summary>
    public bool IsProjected { get; set; }
    /// <summary>Yıllık onaylı emisyon (kgCO2e; IsProjected ise tahmini).</summary>
    public decimal BaselineEmissions { get; set; }
    public decimal ReducedEmissions { get; set; }
    public decimal TotalSaving { get; set; }
    public decimal SavingPercent { get; set; }
    /// <summary>Aktif hedef varsa hedef emisyonu (kgCO2e); senaryonun hedefi karşılayıp karşılamadığını gösterir.</summary>
    public decimal? TargetEmissions { get; set; }
    public int? TargetYear { get; set; }
    public bool? MeetsTarget { get; set; }
    public List<ScenarioCategoryDto> Categories { get; set; } = new();
}

public class ScenarioCategoryDto
{
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int? Scope { get; set; }
    /// <summary>Yıllık tüketim ve emisyon (ScenarioResultDto.IsProjected ise ölçeklenmiş).</summary>
    public decimal Consumption { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal Emissions { get; set; }
    public decimal ReductionPercent { get; set; }
    public decimal Saving { get; set; }
    public string? Label { get; set; }
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
