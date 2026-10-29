using GreenOps.Application.Common;

namespace GreenOps.Application.Abstractions.Services;

public interface IAuditLogService
{
    Task LogAsync(Guid? userId, string userName, string action, string entityName, string? entityId = null, string? oldValues = null, string? newValues = null, CancellationToken cancellationToken = default);
}
