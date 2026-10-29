using GreenOps.Application.Abstractions.Repositories;
using GreenOps.Domain.Entities;
using GreenOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GreenOps.Infrastructure.Repositories;

public class ApiKeyRepository : RepositoryBase<ApiKey>, IApiKeyRepository
{
    public ApiKeyRepository(AppDbContext dbContext) : base(dbContext) { }

    public async Task<IReadOnlyList<ApiKey>> ListByCompanyAsync(Guid companyId, CancellationToken ct = default)
        => await DbContext.ApiKeys.Where(a => a.CompanyId == companyId).OrderByDescending(a => a.CreatedAt).ToListAsync(ct);

    public async Task<ApiKey?> GetByKeyAsync(string key, CancellationToken ct = default)
        => await DbContext.ApiKeys.FirstOrDefaultAsync(a => a.Key == key && a.IsActive, ct);
}
