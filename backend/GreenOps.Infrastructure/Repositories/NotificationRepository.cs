using GreenOps.Application.Abstractions.Repositories;
using GreenOps.Domain.Entities;
using GreenOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GreenOps.Infrastructure.Repositories;

public class NotificationRepository : RepositoryBase<Notification>, INotificationRepository
{
    public NotificationRepository(AppDbContext dbContext) : base(dbContext) { }

    public async Task<IReadOnlyList<Notification>> ListByUserAsync(Guid userId, bool unreadOnly = true, CancellationToken ct = default)
    {
        var query = DbContext.Notifications.Where(n => n.UserId == userId);
        if (unreadOnly) query = query.Where(n => !n.IsRead);
        return await query.OrderByDescending(n => n.CreatedAt).ToListAsync(ct);
    }
}
