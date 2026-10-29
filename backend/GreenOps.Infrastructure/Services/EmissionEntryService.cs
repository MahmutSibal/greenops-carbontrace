using GreenOps.Application.Abstractions.Repositories;
using GreenOps.Application.Common;
using GreenOps.Application.EmissionEntries;
using GreenOps.Application.EmissionEntries.Commands;
using GreenOps.Domain.Entities;
using GreenOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GreenOps.Infrastructure.Services;

public class EmissionEntryService : IEmissionEntryService
{
    private readonly IConversionFactorRepository _conversionFactorRepository;
    private readonly IEmissionEntryRepository _emissionEntryRepository;
    private readonly AppDbContext _dbContext;

    public EmissionEntryService(
        IConversionFactorRepository conversionFactorRepository,
        IEmissionEntryRepository emissionEntryRepository,
        AppDbContext dbContext)
    {
        _conversionFactorRepository = conversionFactorRepository;
        _emissionEntryRepository = emissionEntryRepository;
        _dbContext = dbContext;
    }

    public async Task<Result<Guid>> CreateAsync(EmissionEntry entry, Guid? companyScope, CancellationToken cancellationToken = default)
    {
        // Toplu yükleme MediatR'ı atladığı için temel kurallar burada da kontrol edilir.
        if (entry.Value <= 0 || entry.Value > EmissionLimits.MaxValue)
            return Result<Guid>.Failure("Değer 0 ile 1.000.000 arasında olmalıdır.");
        if (entry.PeriodStart.HasValue && entry.PeriodEnd.HasValue && entry.PeriodEnd < entry.PeriodStart)
            return Result<Guid>.Failure("Dönem bitişi, dönem başlangıcından önce olamaz.");

        var facility = await _dbContext.Facilities
            .FirstOrDefaultAsync(f => f.Id == entry.FacilityId && f.IsActive, cancellationToken);
        if (facility is null || (companyScope.HasValue && facility.CompanyId != companyScope.Value))
            return Result<Guid>.Failure("Tesis bulunamadı.");

        var category = await _dbContext.EmissionCategories
            .FirstOrDefaultAsync(c => c.Id == entry.CategoryId, cancellationToken);
        if (category is null)
            return Result<Guid>.Failure("Emisyon kategorisi bulunamadı.");

        var factorYear = entry.PeriodStart?.Year ?? DateTimeOffset.UtcNow.Year;
        var conversionFactor = await _conversionFactorRepository
            .GetApplicableFactorAsync(entry.CategoryId, factorYear, cancellationToken);
        if (conversionFactor is null)
            return Result<Guid>.Failure($"Seçilen kategori için {factorYear} veya öncesine ait dönüşüm faktörü bulunamadı.");

        entry.Id = Guid.NewGuid();
        entry.CompanyId = facility.CompanyId;
        entry.Unit = category.Unit;
        entry.ConversionFactorId = conversionFactor.Id;
        entry.FactorValue = conversionFactor.Factor;
        entry.CalculationResult = entry.Value * conversionFactor.Factor;
        entry.Status = EmissionEntryStatus.Pending;
        entry.CreatedAt = DateTimeOffset.UtcNow;

        await _emissionEntryRepository.AddAsync(entry, cancellationToken);
        await _emissionEntryRepository.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(entry.Id);
    }

    public async Task<Result<EmissionEntry>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entry = await _dbContext.EmissionEntries
            .Include(e => e.Facility)
            .Include(e => e.Category)
            .Include(e => e.ConversionFactor)
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted, cancellationToken);

        if (entry is null)
            return Result<EmissionEntry>.Failure("Emisyon kaydı bulunamadı.");

        return Result<EmissionEntry>.Success(entry);
    }

    public async Task<Result<PagedResult<EmissionEntry>>> GetPagedAsync(EmissionQuery q, CancellationToken cancellationToken = default)
    {
        var (page, pageSize) = PagedResult<EmissionEntry>.Normalize(q.Page, q.PageSize);

        var query = _dbContext.EmissionEntries.AsNoTracking().Where(e => !e.IsDeleted);

        if (q.CompanyId.HasValue)
            query = query.Where(e => e.CompanyId == q.CompanyId.Value);
        if (q.FacilityId.HasValue)
            query = query.Where(e => e.FacilityId == q.FacilityId.Value);
        if (q.CategoryId.HasValue)
            query = query.Where(e => e.CategoryId == q.CategoryId.Value);
        if (q.From.HasValue)
            query = query.Where(e => (e.PeriodStart ?? e.CreatedAt) >= q.From.Value);
        if (q.To.HasValue)
            query = query.Where(e => (e.PeriodStart ?? e.CreatedAt) <= q.To.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Include(e => e.Facility)
            .Include(e => e.Category)
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return Result<PagedResult<EmissionEntry>>.Success(new PagedResult<EmissionEntry>
        {
            Items = items, TotalCount = total, Page = page, PageSize = pageSize
        });
    }

    public async Task<Result> ApproveAsync(Guid id, Guid approvedByUserId, bool allowSelfApproval, CancellationToken cancellationToken = default)
    {
        var entry = await _emissionEntryRepository.GetByIdAsync(id, cancellationToken);
        if (entry is null || entry.IsDeleted)
            return Result.Failure("Emisyon kaydı bulunamadı.");

        if (entry.Status != EmissionEntryStatus.Pending)
            return Result.Failure("Sadece beklemedeki kayıtlar onaylanabilir.");

        // Görevler ayrılığı: veriyi giren kişi kendi kaydını onaylayamaz.
        if (!allowSelfApproval && entry.CreatedByUserId == approvedByUserId)
            return Result.Failure("Kendi girdiğiniz kaydı onaylayamazsınız.");

        entry.Status = EmissionEntryStatus.Approved;
        entry.ApprovedByUserId = approvedByUserId;
        entry.ApprovedAt = DateTimeOffset.UtcNow;

        _emissionEntryRepository.Update(entry);
        await _emissionEntryRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> UpdateEvidenceUrlAsync(Guid id, string evidenceUrl, CancellationToken cancellationToken = default)
    {
        var entry = await _emissionEntryRepository.GetByIdAsync(id, cancellationToken);
        if (entry is null || entry.IsDeleted)
            return Result.Failure("Emisyon kaydı bulunamadı.");

        entry.EvidenceUrl = evidenceUrl;
        entry.UpdatedAt = DateTimeOffset.UtcNow;
        _emissionEntryRepository.Update(entry);
        await _emissionEntryRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entry = await _emissionEntryRepository.GetByIdAsync(id, cancellationToken);
        if (entry is null || entry.IsDeleted)
            return Result.Failure("Emisyon kaydı bulunamadı.");

        // Onaylanmış veri raporlara girmiş olabilir; silinmez.
        if (entry.Status == EmissionEntryStatus.Approved)
            return Result.Failure("Onaylanmış kayıtlar silinemez.");

        entry.IsDeleted = true;
        entry.UpdatedAt = DateTimeOffset.UtcNow;
        _emissionEntryRepository.Update(entry);
        await _emissionEntryRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> RejectAsync(Guid id, string reason, Guid rejectedByUserId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure("Red nedeni girilmelidir.");
        if (reason.Length > 1000)
            return Result.Failure("Red nedeni en fazla 1000 karakter olabilir.");

        var entry = await _emissionEntryRepository.GetByIdAsync(id, cancellationToken);
        if (entry is null || entry.IsDeleted)
            return Result.Failure("Emisyon kaydı bulunamadı.");

        if (entry.Status != EmissionEntryStatus.Pending)
            return Result.Failure("Sadece beklemedeki kayıtlar reddedilebilir.");

        entry.Status = EmissionEntryStatus.Rejected;
        entry.RejectionReason = reason.Trim();
        entry.RejectedByUserId = rejectedByUserId;
        entry.RejectedAt = DateTimeOffset.UtcNow;

        _emissionEntryRepository.Update(entry);
        await _emissionEntryRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> UpdateAsync(Guid id, decimal? value, Guid? categoryId, CancellationToken cancellationToken = default)
    {
        if (value.HasValue && (value.Value <= 0 || value.Value > EmissionLimits.MaxValue))
            return Result.Failure("Değer 0 ile 1.000.000 arasında olmalıdır.");

        var entry = await _dbContext.EmissionEntries
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted, cancellationToken);

        if (entry is null)
            return Result.Failure("Emisyon kaydı bulunamadı.");

        if (entry.Status != EmissionEntryStatus.Pending)
            return Result.Failure("Sadece onaylanmamış veriler güncellenebilir.");

        if (categoryId.HasValue && categoryId.Value != entry.CategoryId)
        {
            var category = await _dbContext.EmissionCategories
                .FirstOrDefaultAsync(c => c.Id == categoryId.Value, cancellationToken);
            if (category is null)
                return Result.Failure("Emisyon kategorisi bulunamadı.");

            entry.CategoryId = category.Id;
            entry.Unit = category.Unit;
        }

        if (value.HasValue)
            entry.Value = value.Value;

        var factorYear = entry.PeriodStart?.Year ?? entry.CreatedAt.Year;
        var conversionFactor = await _conversionFactorRepository
            .GetApplicableFactorAsync(entry.CategoryId, factorYear, cancellationToken);
        if (conversionFactor is null)
            return Result.Failure($"Seçilen kategori için {factorYear} veya öncesine ait dönüşüm faktörü bulunamadı.");

        entry.ConversionFactorId = conversionFactor.Id;
        entry.FactorValue = conversionFactor.Factor;
        entry.CalculationResult = entry.Value * conversionFactor.Factor;
        entry.UpdatedAt = DateTimeOffset.UtcNow;

        await _emissionEntryRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
