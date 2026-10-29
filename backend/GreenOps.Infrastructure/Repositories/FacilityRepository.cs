using GreenOps.Application.Abstractions.Repositories;
using GreenOps.Domain.Entities;
using GreenOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GreenOps.Infrastructure.Repositories;

public class FacilityRepository : RepositoryBase<Facility>, IFacilityRepository
{
    public FacilityRepository(AppDbContext dbContext) : base(dbContext) { }

    public async Task<IReadOnlyList<Facility>> ListByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return await DbContext.Facilities
            .Where(f => f.CompanyId == companyId && f.IsActive)
            .ToListAsync(cancellationToken);
    }
}
