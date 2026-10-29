using GreenOps.Domain.Entities;

namespace GreenOps.Application.Abstractions.Repositories;

public interface IRoleRepository : IRepository<Role>
{
    Task<IReadOnlyList<Role>> ListSystemRolesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Role>> ListByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);
}
