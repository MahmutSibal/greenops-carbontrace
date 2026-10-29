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
