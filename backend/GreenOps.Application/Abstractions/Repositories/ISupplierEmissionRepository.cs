using GreenOps.Domain.Entities;

namespace GreenOps.Application.Abstractions.Repositories;

public interface ISupplierEmissionRepository : IRepository<SupplierEmission>
{
    Task<IReadOnlyList<SupplierEmission>> ListBySupplierAsync(Guid supplierId, CancellationToken cancellationToken = default);
}
