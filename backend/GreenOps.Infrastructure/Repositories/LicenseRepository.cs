using GreenOps.Application.Abstractions.Repositories;
using GreenOps.Domain.Entities;
using GreenOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GreenOps.Infrastructure.Repositories;

public class LicenseRepository : RepositoryBase<License>, ILicenseRepository
{
    public LicenseRepository(AppDbContext dbContext) : base(dbContext) { }

    public async Task<License?> GetByKeyAsync(string licenseKey, CancellationToken cancellationToken = default)
    {
        return await DbContext.Licenses
            .FirstOrDefaultAsync(l => l.LicenseKey == licenseKey, cancellationToken);
    }

    public async Task<License?> GetActiveByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return await DbContext.Licenses
            .FirstOrDefaultAsync(l => l.CompanyId == companyId && l.IsActive && l.ExpiresAt > DateTimeOffset.UtcNow, cancellationToken);
    }

    public async Task<List<License>> GetAllByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return await DbContext.Licenses
            .Where(l => l.CompanyId == companyId)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
