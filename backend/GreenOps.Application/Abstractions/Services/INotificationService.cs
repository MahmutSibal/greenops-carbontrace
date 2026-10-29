using GreenOps.Application.Common;

namespace GreenOps.Application.Abstractions.Services;

public interface INotificationService
{
    Task<Result<IReadOnlyList<NotificationDto>>> ListByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Result> MarkAsReadAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default);
    Task<Result> CreateNotificationAsync(Guid userId, string title, string message, CancellationToken cancellationToken = default);
    Task<Result> UpdateAlertSettingsAsync(UpdateAlertSettingsRequest request, CancellationToken cancellationToken = default);
}

public class NotificationDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class UpdateAlertSettingsRequest
{
    public Guid CompanyId { get; set; }
    public string AlertType { get; set; } = string.Empty;
    public decimal Threshold { get; set; }
    public bool NotifyEmail { get; set; }
    public bool NotifySms { get; set; }
}
