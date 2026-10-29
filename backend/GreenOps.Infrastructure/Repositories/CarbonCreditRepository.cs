using GreenOps.Application.Abstractions.Repositories;
using GreenOps.Domain.Entities;
using GreenOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GreenOps.Infrastructure.Repositories;

public class CarbonCreditRepository : RepositoryBase<CarbonCredit>, ICarbonCreditRepository
{
    public CarbonCreditRepository(AppDbContext dbContext) : base(dbContext) { }

    public async Task<IReadOnlyList<CarbonCredit>> ListByCompanyAsync(Guid companyId, CancellationToken ct = default)
        => await DbContext.CarbonCredits.Where(c => c.CompanyId == companyId).OrderByDescending(c => c.PurchaseDate).ToListAsync(ct);
}
