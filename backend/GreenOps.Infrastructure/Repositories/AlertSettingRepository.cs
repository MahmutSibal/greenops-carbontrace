using GreenOps.Application.Abstractions.Repositories;
using GreenOps.Domain.Entities;
using GreenOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GreenOps.Infrastructure.Repositories;

public class AlertSettingRepository : RepositoryBase<AlertSetting>, IAlertSettingRepository
{
    public AlertSettingRepository(AppDbContext dbContext) : base(dbContext) { }

    public async Task<IReadOnlyList<AlertSetting>> ListByCompanyAsync(Guid companyId, CancellationToken ct = default)
        => await DbContext.AlertSettings.Where(a => a.CompanyId == companyId && a.IsActive).ToListAsync(ct);
}
