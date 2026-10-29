using GreenOps.Application.Abstractions.Repositories;
using GreenOps.Application.Abstractions.Services;
using GreenOps.Application.Common;
using GreenOps.Domain.Entities;

namespace GreenOps.Infrastructure.Services;

public class SupplierService : ISupplierService
{
    private readonly ISupplierRepository _supplierRepository;
    private readonly ISupplierEmissionRepository _emissionRepository;

    public SupplierService(ISupplierRepository supplierRepository, ISupplierEmissionRepository emissionRepository)
    {
        _supplierRepository = supplierRepository;
        _emissionRepository = emissionRepository;
    }

    public async Task<Result<IReadOnlyList<SupplierDto>>> ListByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var suppliers = await _supplierRepository.ListByCompanyAsync(companyId, cancellationToken);
        var dtos = suppliers.Select(s => new SupplierDto
        {
            Id = s.Id,
            Name = s.Name,
            ContactEmail = s.ContactEmail,
            ESGScore = s.ESGScore,
            IsActive = s.IsActive
        }).ToList();

        return Result<IReadOnlyList<SupplierDto>>.Success(dtos);
    }

    public async Task<Result> InviteSupplierAsync(InviteSupplierRequest request, CancellationToken cancellationToken = default)
    {
        var supplier = new Supplier
        {
            Id = Guid.NewGuid(),
            CompanyId = request.CompanyId,
            Name = request.Name,
            ContactEmail = request.ContactEmail,
            ESGScore = 0,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _supplierRepository.AddAsync(supplier, cancellationToken);
        await _supplierRepository.SaveChangesAsync(cancellationToken);

        // TODO: Send invitation email to supplier

        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<SupplierEmissionDto>>> GetSupplierEmissionsAsync(Guid supplierId, CancellationToken cancellationToken = default)
    {
        var emissions = await _emissionRepository.ListBySupplierAsync(supplierId, cancellationToken);
        var dtos = emissions.Select(e => new SupplierEmissionDto
        {
            Id = e.Id,
            Year = e.Year,
            Value = e.Value,
            Description = e.Description
        }).ToList();

        return Result<IReadOnlyList<SupplierEmissionDto>>.Success(dtos);
    }

    public async Task<Result> UpdateESGScoreAsync(Guid supplierId, int score, CancellationToken cancellationToken = default)
    {
        if (score < 0 || score > 100)
            return Result.Failure("ESG puanı 0-100 arasında olmalıdır.");

        var supplier = await _supplierRepository.GetByIdAsync(supplierId, cancellationToken);
        if (supplier is null)
            return Result.Failure("Tedarikçi bulunamadı.");

        supplier.ESGScore = score;
        _supplierRepository.Update(supplier);
        await _supplierRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
