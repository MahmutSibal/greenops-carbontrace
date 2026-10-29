using GreenOps.Domain.Entities;

namespace GreenOps.Application.Abstractions.Repositories;

public interface ITargetRepository : IRepository<Target>
{
    Task<IReadOnlyList<Target>> ListByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);
}
