using GreenOps.Application.Abstractions.Repositories;
using GreenOps.Domain.Entities;
using GreenOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GreenOps.Infrastructure.Repositories;

public class RoleRepository : RepositoryBase<Role>, IRoleRepository
{
    public RoleRepository(AppDbContext dbContext) : base(dbContext) { }

    public async Task<IReadOnlyList<Role>> ListSystemRolesAsync(CancellationToken ct = default)
        => await DbContext.Roles.Where(r => r.IsSystem).ToListAsync(ct);

    public async Task<IReadOnlyList<Role>> ListByCompanyAsync(Guid companyId, CancellationToken ct = default)
        => await DbContext.Roles.Where(r => r.IsSystem || r.CompanyId == companyId).ToListAsync(ct);
}
