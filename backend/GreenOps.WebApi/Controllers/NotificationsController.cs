using GreenOps.Application.Abstractions.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GreenOps.WebApi.Controllers;

[Route("api/notifications")]
[Authorize]
public class NotificationsController : ApiControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    /// <summary>GET /api/notifications — Kullanıcının bildirimlerini listeler</summary>
    [HttpGet]
    public async Task<IActionResult> ListNotifications(CancellationToken ct)
    {
        if (CurrentUserId is not { } userId) return Unauthorized();
        var result = await _notificationService.ListByUserAsync(userId, ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

    /// <summary>PUT /api/notifications/{id}/read — Kendi bildirimini okundu işaretler</summary>
    [HttpPut("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken ct)
    {
        if (CurrentUserId is not { } userId) return Unauthorized();
        var result = await _notificationService.MarkAsReadAsync(id, userId, ct);
        if (!result.IsSuccess) return NotFoundError(result.Error!);
        return Ok(new { message = "Bildirim okundu olarak işaretlendi." });
    }
}

[Route("api/alerts")]
[Authorize(Roles = AdminRoles)]
public class AlertsController : ApiControllerBase
{
    private readonly INotificationService _notificationService;

    public AlertsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    /// <summary>POST /api/alerts/settings — Şirketin alarm ayarlarını günceller (SuperAdmin, CompanyAdmin)</summary>
    [HttpPost("settings")]
    public async Task<IActionResult> UpdateAlertSettings([FromBody] UpdateAlertSettingsRequest request, CancellationToken ct)
    {
        if (!TryResolveCompany(request.CompanyId, out var cid)) return CompanyForbidden();
        if (string.IsNullOrWhiteSpace(request.AlertType) || request.AlertType.Length > 100)
            return BadRequest(new { error = "Alarm türü 1-100 karakter olmalıdır." });
        if (request.Threshold < 0)
            return BadRequest(new { error = "Eşik değeri negatif olamaz." });

        request.CompanyId = cid;
        var result = await _notificationService.UpdateAlertSettingsAsync(request, ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(new { message = "Alarm ayarları güncellendi." });
    }
}
