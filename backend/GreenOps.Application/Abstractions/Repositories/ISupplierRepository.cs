using GreenOps.Domain.Entities;

namespace GreenOps.Application.Abstractions.Repositories;

public interface ISupplierRepository : IRepository<Supplier>
{
    Task<IReadOnlyList<Supplier>> ListByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);
}
