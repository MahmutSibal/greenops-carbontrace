using GreenOps.Application.Common;
using GreenOps.Domain.Entities;

namespace GreenOps.Application.Abstractions.Repositories;

public interface IAuditLogRepository : IRepository<AuditLog>
{
    Task<PagedResult<AuditLog>> GetPagedAsync(Guid? companyId, int page, int pageSize, CancellationToken cancellationToken = default);
}
