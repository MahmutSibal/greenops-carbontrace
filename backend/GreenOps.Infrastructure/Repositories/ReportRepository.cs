using GreenOps.Application.Abstractions.Repositories;
using GreenOps.Domain.Entities;
using GreenOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GreenOps.Infrastructure.Repositories;

public class ReportRepository : RepositoryBase<Report>, IReportRepository
{
    public ReportRepository(AppDbContext dbContext) : base(dbContext) { }

    public async Task<IReadOnlyList<Report>> ListByCompanyAsync(Guid companyId, CancellationToken ct = default)
        => await DbContext.Reports.Where(r => r.CompanyId == companyId).OrderByDescending(r => r.CreatedAt).ToListAsync(ct);
}
