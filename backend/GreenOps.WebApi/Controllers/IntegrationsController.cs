using GreenOps.Application.Abstractions.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GreenOps.WebApi.Controllers;

[Route("api/integrations")]
[Authorize(Roles = AdminRoles)]
public class IntegrationsController : ApiControllerBase
{
    private readonly IIntegrationService _integrationService;

    public IntegrationsController(IIntegrationService integrationService)
    {
        _integrationService = integrationService;
    }

    /// <summary>POST /api/integrations/api-keys — API anahtarı üretir (düz anahtar sadece bu yanıtta görünür)</summary>
    [HttpPost("api-keys")]
    public async Task<IActionResult> CreateApiKey([FromBody] CreateApiKeyRequest request, CancellationToken ct)
    {
        if (!TryResolveCompany(request.CompanyId, out var cid)) return CompanyForbidden();
        var result = await _integrationService.CreateApiKeyAsync(cid, request.Name, ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

    /// <summary>GET /api/integrations/api-keys — Şirketin API anahtarlarını listeler (anahtar değeri olmadan)</summary>
    [HttpGet("api-keys")]
    public async Task<IActionResult> ListApiKeys([FromQuery] Guid? companyId, CancellationToken ct)
    {
        if (!TryResolveCompany(companyId, out var cid)) return CompanyForbidden();
        var result = await _integrationService.ListApiKeysAsync(cid, ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

    /// <summary>GET /api/integrations/status — Entegrasyon bağlantı durumu</summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetStatus([FromQuery] Guid? companyId, CancellationToken ct)
    {
        if (!TryResolveCompany(companyId, out var cid)) return CompanyForbidden();
        var result = await _integrationService.GetStatusAsync(cid, ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }
}

/// <summary>Cihazlar kullanıcı JWT'si taşıyamaz; X-Api-Key başlığıyla kimlik doğrular.</summary>
[Route("api/webhooks/iot-meters")]
[AllowAnonymous]
[EnableRateLimiting(RateLimitPolicies.Webhook)]
public class IotMetersController : ApiControllerBase
{
    public const string ApiKeyHeader = "X-Api-Key";

    private readonly IIntegrationService _integrationService;

    public IotMetersController(IIntegrationService integrationService)
    {
        _integrationService = integrationService;
    }

    /// <summary>POST /api/webhooks/iot-meters — IoT sayaçlarından veri kabul eder</summary>
    [HttpPost]
    public async Task<IActionResult> ReceiveMeterData([FromHeader(Name = ApiKeyHeader)] string? apiKey, [FromBody] IotMeterDataRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(apiKey)) return Unauthorized(new { error = $"{ApiKeyHeader} başlığı gerekli." });

        var result = await _integrationService.ReceiveIotMeterDataAsync(apiKey, request, ct);
        if (!result.IsSuccess)
            return result.Error == "Geçersiz API anahtarı."
                ? Unauthorized(new { error = result.Error })
                : BadRequest(new { error = result.Error });
        return Ok(new { message = "IoT verisi alındı ve kaydedildi." });
    }
}

public class CreateApiKeyRequest { public Guid? CompanyId { get; set; } public string Name { get; set; } = string.Empty; }
