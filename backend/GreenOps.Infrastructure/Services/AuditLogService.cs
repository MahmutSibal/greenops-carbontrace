using GreenOps.Application.Abstractions.Repositories;
using GreenOps.Application.Abstractions.Services;
using GreenOps.Domain.Entities;

namespace GreenOps.Infrastructure.Services;

public class AuditLogService : IAuditLogService
{
    private readonly IAuditLogRepository _auditLogRepository;

    public AuditLogService(IAuditLogRepository auditLogRepository)
    {
        _auditLogRepository = auditLogRepository;
    }

    public async Task LogAsync(Guid? userId, string userName, string action, string entityName, string? entityId = null, string? oldValues = null, string? newValues = null, CancellationToken cancellationToken = default)
    {
        var log = new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            UserName = userName,
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            OldValues = oldValues,
            NewValues = newValues,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _auditLogRepository.AddAsync(log, cancellationToken);
        await _auditLogRepository.SaveChangesAsync(cancellationToken);
    }
}
