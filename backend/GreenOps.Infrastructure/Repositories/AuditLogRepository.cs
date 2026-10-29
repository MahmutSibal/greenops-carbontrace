using GreenOps.Application.Abstractions.Repositories;
using GreenOps.Application.Common;
using GreenOps.Domain.Entities;
using GreenOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GreenOps.Infrastructure.Repositories;

public class AuditLogRepository : RepositoryBase<AuditLog>, IAuditLogRepository
{
    public AuditLogRepository(AppDbContext dbContext) : base(dbContext) { }

    public async Task<PagedResult<AuditLog>> GetPagedAsync(Guid? companyId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PagedResult<AuditLog>.Normalize(page, pageSize);

        var query = DbContext.AuditLogs.AsNoTracking();
        if (companyId.HasValue)
            query = query.Where(l => l.CompanyId == companyId.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<AuditLog> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }
}
