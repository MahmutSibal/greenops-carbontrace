using GreenOps.Domain.Entities;

namespace GreenOps.Application.Abstractions.Repositories;

public interface INotificationRepository : IRepository<Notification>
{
    Task<IReadOnlyList<Notification>> ListByUserAsync(Guid userId, bool unreadOnly = true, CancellationToken cancellationToken = default);
}
