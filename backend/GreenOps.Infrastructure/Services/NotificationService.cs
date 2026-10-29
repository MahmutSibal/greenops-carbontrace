using GreenOps.Application.Abstractions.Repositories;
using GreenOps.Application.Abstractions.Services;
using GreenOps.Application.Common;
using GreenOps.Domain.Entities;

namespace GreenOps.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepository;
    private readonly IAlertSettingRepository _alertSettingRepository;

    public NotificationService(INotificationRepository notificationRepository, IAlertSettingRepository alertSettingRepository)
    {
        _notificationRepository = notificationRepository;
        _alertSettingRepository = alertSettingRepository;
    }

    public async Task<Result<IReadOnlyList<NotificationDto>>> ListByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var notifications = await _notificationRepository.ListByUserAsync(userId, unreadOnly: false, cancellationToken);
        var dtos = notifications.Select(n => new NotificationDto
        {
            Id = n.Id,
            Title = n.Title,
            Message = n.Message,
            IsRead = n.IsRead,
            CreatedAt = n.CreatedAt
        }).ToList();

        return Result<IReadOnlyList<NotificationDto>>.Success(dtos);
    }

    public async Task<Result> MarkAsReadAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default)
    {
        var notification = await _notificationRepository.GetByIdAsync(notificationId, cancellationToken);
        if (notification is null || notification.UserId != userId)
            return Result.Failure("Bildirim bulunamadı.");

        notification.IsRead = true;
        notification.ReadAt = DateTimeOffset.UtcNow;
        _notificationRepository.Update(notification);
        await _notificationRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> CreateNotificationAsync(Guid userId, string title, string message, CancellationToken cancellationToken = default)
    {
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = title,
            Message = message,
            IsRead = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _notificationRepository.AddAsync(notification, cancellationToken);
        await _notificationRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> UpdateAlertSettingsAsync(UpdateAlertSettingsRequest request, CancellationToken cancellationToken = default)
    {
        var existing = (await _alertSettingRepository.ListByCompanyAsync(request.CompanyId, cancellationToken))
            .FirstOrDefault(a => a.AlertType == request.AlertType);

        if (existing is not null)
        {
            existing.Threshold = request.Threshold;
            existing.NotifyEmail = request.NotifyEmail;
            existing.NotifySms = request.NotifySms;
            _alertSettingRepository.Update(existing);
        }
        else
        {
            var alertSetting = new AlertSetting
            {
                Id = Guid.NewGuid(),
                CompanyId = request.CompanyId,
                AlertType = request.AlertType,
                Threshold = request.Threshold,
                NotifyEmail = request.NotifyEmail,
                NotifySms = request.NotifySms,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            };
            await _alertSettingRepository.AddAsync(alertSetting, cancellationToken);
        }

        await _alertSettingRepository.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
