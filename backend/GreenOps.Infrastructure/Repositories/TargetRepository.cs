using GreenOps.Application.Abstractions.Repositories;
using GreenOps.Domain.Entities;
using GreenOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GreenOps.Infrastructure.Repositories;

public class TargetRepository : RepositoryBase<Target>, ITargetRepository
{
    public TargetRepository(AppDbContext dbContext) : base(dbContext) { }

    public async Task<IReadOnlyList<Target>> ListByCompanyAsync(Guid companyId, CancellationToken ct = default)
        => await DbContext.Targets.Where(t => t.CompanyId == companyId && t.IsActive).OrderByDescending(t => t.TargetYear).ToListAsync(ct);
}
