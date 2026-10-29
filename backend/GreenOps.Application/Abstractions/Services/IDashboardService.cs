using GreenOps.Application.Common;

namespace GreenOps.Application.Abstractions.Services;

public interface IDashboardService
{
    Task<Result<DashboardSummary>> GetSummaryAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<FacilityComparison>>> GetFacilityComparisonAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<TrendPoint>>> GetTrendsAsync(Guid companyId, int? year, CancellationToken cancellationToken = default);
}

public class DashboardSummary
{
    public decimal TotalEmissions { get; set; }
    public decimal Scope1Total { get; set; }
    public decimal Scope2Total { get; set; }
    public decimal Scope3Total { get; set; }
    public int FacilityCount { get; set; }
    public int PendingEntries { get; set; }
    public decimal MonthlyChange { get; set; }
    public decimal CarbonIntensity { get; set; }
}

public class FacilityComparison
{
    public Guid FacilityId { get; set; }
    public string FacilityName { get; set; } = string.Empty;
    public decimal TotalEmissions { get; set; }
}

public class TrendPoint
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal TotalEmissions { get; set; }
}
