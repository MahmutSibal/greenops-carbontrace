using GreenOps.Application.Abstractions.Services;
using GreenOps.Application.Common;
using GreenOps.Domain.Entities;
using GreenOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GreenOps.Infrastructure.Services;

/// <remarks>
/// Toplamlar kaydın şirketine (EmissionEntry.CompanyId) göre alınır; böylece pasife alınan tesislerin
/// geçmiş emisyonları kaybolmaz. Zaman ekseni faaliyet dönemidir (yoksa giriş tarihi).
/// </remarks>
public class DashboardService : IDashboardService
{
    private readonly AppDbContext _dbContext;

    public DashboardService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    private IQueryable<EmissionEntry> Approved(Guid companyId) =>
        _dbContext.EmissionEntries.AsNoTracking()
            .Where(e => e.CompanyId == companyId && e.Status == EmissionEntryStatus.Approved && !e.IsDeleted);

    public async Task<Result<DashboardSummary>> GetSummaryAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var approved = Approved(companyId);

        var byScope = await approved
            .GroupBy(e => e.Category!.Scope!.Number)
            .Select(g => new { Scope = (int?)g.Key, Total = g.Sum(e => e.CalculationResult) })
            .ToListAsync(cancellationToken);

        decimal ScopeTotal(int n) => byScope.Where(s => s.Scope == n).Sum(s => s.Total);
        var totalEmissions = byScope.Sum(s => s.Total);

        var pending = await _dbContext.EmissionEntries
            .CountAsync(e => e.CompanyId == companyId && e.Status == EmissionEntryStatus.Pending && !e.IsDeleted, cancellationToken);

        // Son TAMAMLANMIŞ ay bir öncekiyle kıyaslanır; içinde bulunulan yarım ay
        // (henüz faturası gelmemiş) kıyaslanırsa değişim yanıltıcı biçimde -%100 çıkıyordu.
        var now = DateTimeOffset.UtcNow;
        var nextMonthStart = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var thisMonthStart = nextMonthStart.AddMonths(-1);
        var lastMonthStart = thisMonthStart.AddMonths(-1);

        var thisMonth = await approved
            .Where(e => (e.PeriodStart ?? e.CreatedAt) >= thisMonthStart && (e.PeriodStart ?? e.CreatedAt) < nextMonthStart)
            .SumAsync(e => e.CalculationResult, cancellationToken);
        var lastMonth = await approved
            .Where(e => (e.PeriodStart ?? e.CreatedAt) >= lastMonthStart && (e.PeriodStart ?? e.CreatedAt) < thisMonthStart)
            .SumAsync(e => e.CalculationResult, cancellationToken);
        var monthlyChange = lastMonth > 0 ? Math.Round((thisMonth - lastMonth) / lastMonth * 100, 1) : 0;

        var activeFacilities = _dbContext.Facilities.Where(f => f.CompanyId == companyId && f.IsActive);
        var facilityCount = await activeFacilities.CountAsync(cancellationToken);
        var totalEmployees = await activeFacilities
            .Where(f => f.EmployeeCount.HasValue)
            .SumAsync(f => f.EmployeeCount!.Value, cancellationToken);
        var carbonIntensity = totalEmployees > 0 ? Math.Round(totalEmissions / totalEmployees, 2) : 0;

        return Result<DashboardSummary>.Success(new DashboardSummary
        {
            TotalEmissions = totalEmissions,
            Scope1Total = ScopeTotal(1),
            Scope2Total = ScopeTotal(2),
            Scope3Total = ScopeTotal(3),
            FacilityCount = facilityCount,
            PendingEntries = pending,
            MonthlyChange = monthlyChange,
            CarbonIntensity = carbonIntensity
        });
    }

    public async Task<Result<IReadOnlyList<FacilityComparison>>> GetFacilityComparisonAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var result = await _dbContext.Facilities
            .Where(f => f.CompanyId == companyId && f.IsActive)
            .Select(f => new FacilityComparison
            {
                FacilityId = f.Id,
                FacilityName = f.Name,
                TotalEmissions = f.EmissionEntries
                    .Where(e => e.Status == EmissionEntryStatus.Approved && !e.IsDeleted)
                    .Sum(e => e.CalculationResult)
            })
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<FacilityComparison>>.Success(result);
    }

    public async Task<Result<IReadOnlyList<TrendPoint>>> GetTrendsAsync(Guid companyId, int? year, CancellationToken cancellationToken = default)
    {
        var query = Approved(companyId);

        if (year.HasValue)
        {
            var start = new DateTimeOffset(year.Value, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var end = start.AddYears(1);
            query = query.Where(e => (e.PeriodStart ?? e.CreatedAt) >= start && (e.PeriodStart ?? e.CreatedAt) < end);
        }

        var trends = await query
            .Select(e => new { Date = e.PeriodStart ?? e.CreatedAt, e.CalculationResult })
            .GroupBy(e => new { e.Date.Year, e.Date.Month })
            .Select(g => new TrendPoint
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                TotalEmissions = g.Sum(e => e.CalculationResult)
            })
            .OrderBy(t => t.Year).ThenBy(t => t.Month)
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<TrendPoint>>.Success(trends);
    }
}
