using GreenOps.Application.Abstractions.Repositories;
using GreenOps.Application.Abstractions.Services;
using GreenOps.Application.Common;
using GreenOps.Domain.Entities;
using GreenOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GreenOps.Infrastructure.Services;

public class TargetService : ITargetService
{
    private readonly ITargetRepository _targetRepository;
    private readonly ICarbonCreditRepository _creditRepository;
    private readonly AppDbContext _dbContext;

    public TargetService(ITargetRepository targetRepository, ICarbonCreditRepository creditRepository, AppDbContext dbContext)
    {
        _targetRepository = targetRepository;
        _creditRepository = creditRepository;
        _dbContext = dbContext;
    }

    public async Task<Result<IReadOnlyList<TargetDto>>> ListByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var targets = await _targetRepository.ListByCompanyAsync(companyId, cancellationToken);
        var dtos = targets.Select(t => new TargetDto
        {
            Id = t.Id,
            TargetYear = t.TargetYear,
            ReductionPercent = t.ReductionPercent,
            BaselineYear = t.BaselineYear,
            BaselineEmissions = t.BaselineEmissions,
            Description = t.Description
        }).ToList();

        return Result<IReadOnlyList<TargetDto>>.Success(dtos);
    }

    public async Task<Result<Guid>> CreateTargetAsync(CreateTargetRequest request, CancellationToken cancellationToken = default)
    {
        if (request.TargetYear <= DateTime.UtcNow.Year)
            return Result<Guid>.Failure($"Hedef yılı en az {DateTime.UtcNow.Year + 1} olmalıdır.");

        if (request.ReductionPercent <= 0 || request.ReductionPercent > 100)
            return Result<Guid>.Failure("Azaltım yüzdesi 1-100 arasında olmalıdır.");

        if (request.BaselineEmissions <= 0)
            return Result<Guid>.Failure("Baz yıl emisyonları 0'dan büyük olmalıdır.");

        if (request.BaselineYear < 1990 || request.BaselineYear >= request.TargetYear)
            return Result<Guid>.Failure("Baz yıl 1990 ile hedef yılı arasında olmalıdır.");

        if (request.Description?.Length > 500)
            return Result<Guid>.Failure("Açıklama en fazla 500 karakter olabilir.");

        var target = new Target
        {
            Id = Guid.NewGuid(),
            CompanyId = request.CompanyId,
            TargetYear = request.TargetYear,
            ReductionPercent = request.ReductionPercent,
            BaselineYear = request.BaselineYear,
            BaselineEmissions = request.BaselineEmissions,
            Description = request.Description,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _targetRepository.AddAsync(target, cancellationToken);
        await _targetRepository.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(target.Id);
    }

    public async Task<Result<TargetProgressDto>> GetProgressAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var latestTarget = await _dbContext.Targets
            .Where(t => t.CompanyId == companyId && t.IsActive)
            .OrderByDescending(t => t.TargetYear)
            .FirstOrDefaultAsync(cancellationToken);

        if (latestTarget is null)
            return Result<TargetProgressDto>.Success(new TargetProgressDto
            {
                BaselineEmissions = 0,
                CurrentEmissions = 0,
                TargetEmissions = 0,
                ProgressPercent = 0,
                CarbonCreditsOffset = 0
            });

        // Baz yıl yıllık bir değerdir; karşılaştırma da içinde bulunulan yılın emisyonuyla yapılır
        // (önceden tüm zamanların toplamı kullanılıyordu).
        var yearStart = new DateTimeOffset(DateTimeOffset.UtcNow.Year, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var yearEnd = yearStart.AddYears(1);

        var monthly = await _dbContext.EmissionEntries
            .Where(e => e.CompanyId == companyId && e.Status == EmissionEntryStatus.Approved && !e.IsDeleted
                        && (e.PeriodStart ?? e.CreatedAt) >= yearStart && (e.PeriodStart ?? e.CreatedAt) < yearEnd)
            .Select(e => new { Date = e.PeriodStart ?? e.CreatedAt, e.CalculationResult })
            .GroupBy(e => e.Date.Month)
            .Select(g => g.Sum(x => x.CalculationResult))
            .ToListAsync(cancellationToken);

        var currentEmissions = monthly.Sum();
        var monthsCovered = monthly.Count;

        // Baz yıl ve hedef YILLIK değerlerdir. Yıl içinde birkaç aylık toplamı yıllık hedefle kıyaslamak
        // yapay olarak "hedefe ulaşıldı" sonucu veriyordu; eksik yıl, veri olan ayların ortalamasıyla 12 aya ölçeklenir.
        var isProjected = monthsCovered is > 0 and < 12;
        var annualEmissions = isProjected ? currentEmissions / monthsCovered * 12 : currentEmissions;

        // Karbon kredileri tCO2e, emisyonlar kgCO2e cinsindendir.
        var creditsOffset = await _dbContext.CarbonCredits
            .Where(c => c.CompanyId == companyId && c.PurchaseDate >= yearStart && c.PurchaseDate < yearEnd)
            .SumAsync(c => c.Credits, cancellationToken) * 1000m;

        var targetEmissions = latestTarget.BaselineEmissions * (1 - latestTarget.ReductionPercent / 100m);
        var netEmissions = annualEmissions - creditsOffset;
        var reductionNeeded = latestTarget.BaselineEmissions - targetEmissions;
        var reductionAchieved = latestTarget.BaselineEmissions - netEmissions;
        var progressPercent = monthsCovered == 0
            ? 0
            : reductionNeeded > 0 ? Math.Clamp(reductionAchieved / reductionNeeded * 100, 0, 100) : 100;

        return Result<TargetProgressDto>.Success(new TargetProgressDto
        {
            BaselineEmissions = latestTarget.BaselineEmissions,
            CurrentEmissions = currentEmissions,
            ProjectedAnnualEmissions = Math.Round(annualEmissions, 1),
            MonthsCovered = monthsCovered,
            IsProjected = isProjected,
            TargetEmissions = targetEmissions,
            ProgressPercent = Math.Round(progressPercent, 2),
            CarbonCreditsOffset = creditsOffset
        });
    }

    public async Task<Result<ScenarioResultDto>> SimulateScenarioAsync(ScenarioRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Measures.Count > 20)
            return Result<ScenarioResultDto>.Failure("En fazla 20 önlem girilebilir.");
        if (request.Measures.Any(m => m.ReductionPercent < 0 || m.ReductionPercent > 100))
            return Result<ScenarioResultDto>.Failure("Azaltım yüzdesi 0-100 arasında olmalıdır.");
        if (request.Measures.Any(m => m.Label?.Length > 100))
            return Result<ScenarioResultDto>.Failure("Önlem adı en fazla 100 karakter olabilir.");
        if (request.Measures.GroupBy(m => m.CategoryId).Any(g => g.Count() > 1))
            return Result<ScenarioResultDto>.Failure("Her kategori için tek önlem girilebilir.");

        // Son 12 tam ay: yarım kalan bu ay, yıllık değeri olduğundan düşük gösterirdi.
        var now = DateTimeOffset.UtcNow;
        var windowEnd = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var windowStart = windowEnd.AddYears(-1);

        var rows = await _dbContext.EmissionEntries.AsNoTracking()
            .Where(e => e.CompanyId == request.CompanyId && e.Status == EmissionEntryStatus.Approved && !e.IsDeleted
                        && (e.PeriodStart ?? e.CreatedAt) >= windowStart && (e.PeriodStart ?? e.CreatedAt) < windowEnd)
            .Select(e => new
            {
                Date = e.PeriodStart ?? e.CreatedAt,
                e.CategoryId,
                Category = e.Category!.Name,
                Unit = e.Unit ?? e.Category!.Unit,
                Scope = (int?)e.Category!.Scope!.Number,
                e.Value,
                e.CalculationResult
            })
            .ToListAsync(cancellationToken);

        // Hedef yıllıktır; 12 aydan az veri varsa, veri olan ayların ortalamasıyla 12 aya ölçeklenir
        // (birkaç aylık toplamı yıllık hedefle kıyaslamak yapay olarak "hedef karşılanıyor" sonucu verir).
        var monthsWithData = rows.Select(r => (r.Date.Year, r.Date.Month)).Distinct().Count();
        var annualize = monthsWithData is > 0 and < 12 ? 12m / monthsWithData : 1m;

        var measures = request.Measures.ToDictionary(m => m.CategoryId);
        var categories = rows
            .GroupBy(r => r.CategoryId)
            .Select(g =>
            {
                measures.TryGetValue(g.Key, out var m);
                var emissions = g.Sum(x => x.CalculationResult) * annualize;
                var pct = m?.ReductionPercent ?? 0;
                return new ScenarioCategoryDto
                {
                    CategoryId = g.Key,
                    CategoryName = g.First().Category,
                    Scope = g.First().Scope,
                    Consumption = Math.Round(g.Sum(x => x.Value) * annualize, 1),
                    Unit = g.First().Unit,
                    Emissions = Math.Round(emissions, 1),
                    ReductionPercent = pct,
                    // Emisyon = tüketim × sabit katsayı olduğundan tüketimdeki % azalma emisyona aynen yansır.
                    Saving = Math.Round(emissions * pct / 100m, 1),
                    Label = m?.Label
                };
            })
            .OrderByDescending(c => c.Emissions)
            .ToList();

        var baseline = categories.Sum(c => c.Emissions);
        var saving = categories.Sum(c => c.Saving);

        var target = await _dbContext.Targets.AsNoTracking()
            .Where(t => t.CompanyId == request.CompanyId && t.IsActive)
            .OrderByDescending(t => t.TargetYear)
            .Select(t => new { t.TargetYear, Emissions = t.BaselineEmissions * (1 - t.ReductionPercent / 100m) })
            .FirstOrDefaultAsync(cancellationToken);

        return Result<ScenarioResultDto>.Success(new ScenarioResultDto
        {
            WindowStart = windowStart,
            WindowEnd = windowEnd,
            MonthsWithData = monthsWithData,
            IsProjected = annualize != 1m,
            BaselineEmissions = baseline,
            ReducedEmissions = baseline - saving,
            TotalSaving = saving,
            SavingPercent = baseline > 0 ? Math.Round(saving / baseline * 100, 1) : 0,
            TargetEmissions = target is null ? null : Math.Round(target.Emissions, 1),
            TargetYear = target?.TargetYear,
            MeetsTarget = target is null || baseline == 0 ? null : baseline - saving <= target.Emissions,
            Categories = categories
        });
    }

    public async Task<Result> PurchaseCarbonCreditAsync(PurchaseCreditRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Credits <= 0)
            return Result.Failure("Kredi miktarı 0'dan büyük olmalıdır.");

        if (request.Credits > 1_000_000)
            return Result.Failure("Kredi miktarı 1.000.000'u geçemez.");

        var credit = new CarbonCredit
        {
            Id = Guid.NewGuid(),
            CompanyId = request.CompanyId,
            Credits = request.Credits,
            CertificateUrl = request.CertificateUrl,
            Provider = request.Provider,
            PurchaseDate = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _creditRepository.AddAsync(credit, cancellationToken);
        await _creditRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
