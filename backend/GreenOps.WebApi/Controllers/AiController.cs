using GreenOps.Application.Abstractions.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GreenOps.WebApi.Controllers;

/// <summary>Yerel Ollama modelleri (qwen3-vl: görsel OCR, gemma4: analiz ve sohbet) ile AI destekli işlemler.</summary>
[Route("api/ai")]
[Authorize]
public class AiController : ApiControllerBase
{
    private static readonly string[] ImageExtensions = { ".png", ".jpg", ".jpeg", ".webp" };
    private const long MaxImageBytes = 8 * 1024 * 1024;

    private readonly IAiService _aiService;

    public AiController(IAiService aiService)
    {
        _aiService = aiService;
    }

    /// <summary>GET /api/ai/status — Ollama ve modellerin durumu</summary>
    [HttpGet("status")]
    public async Task<IActionResult> Status(CancellationToken ct) => Ok(await _aiService.GetStatusAsync(ct));

    /// <summary>POST /api/ai/invoice-scan — Fatura/sayaç fotoğrafından tüketim, birim ve dönem önerir (kayıt oluşturmaz)</summary>
    [HttpPost("invoice-scan")]
    [RequestSizeLimit(MaxImageBytes + 1024 * 1024)]
    [EnableRateLimiting(RateLimitPolicies.Ai)]
    public async Task<IActionResult> InvoiceScan(IFormFile file, CancellationToken ct)
    {
        if (CurrentCompanyId is not { } companyId && !IsSuperAdmin) return Forbid();
        if (file is null || file.Length == 0) return BadRequest(new { error = "Görsel seçilmedi." });
        if (file.Length > MaxImageBytes) return BadRequest(new { error = "Görsel en fazla 8 MB olabilir." });
        if (!ImageExtensions.Contains(Path.GetExtension(file.FileName).ToLowerInvariant()))
            return BadRequest(new { error = "Desteklenen biçimler: png, jpg, webp." });

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);

        var result = await _aiService.ExtractInvoiceAsync(CurrentCompanyId ?? Guid.Empty, ms.ToArray(), ct);
        if (!result.IsSuccess) return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = result.Error });
        return Ok(result.Value);
    }

    /// <summary>GET /api/ai/insights — Tahmin, anomali ve öneriler (30 dk önbellek; refresh=true ile yenilenir)</summary>
    [HttpGet("insights")]
    [EnableRateLimiting(RateLimitPolicies.Ai)]
    public async Task<IActionResult> Insights([FromQuery] Guid? companyId, [FromQuery] bool refresh = false, CancellationToken ct = default)
    {
        if (!TryResolveCompany(companyId, out var cid)) return CompanyForbidden();
        var result = await _aiService.GetInsightsAsync(cid, refresh, ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

    /// <summary>POST /api/ai/chat — Şirket verisine dayalı karbon asistanı</summary>
    [HttpPost("chat")]
    [EnableRateLimiting(RateLimitPolicies.Ai)]
    public async Task<IActionResult> Chat([FromBody] AiChatRequest request, CancellationToken ct)
    {
        if (!TryResolveCompany(request.CompanyId, out var cid)) return CompanyForbidden();
        var userName = User.Identity?.Name ?? "Kullanıcı";
        var result = await _aiService.ChatAsync(cid, userName, request.History ?? new List<ChatTurn>(), request.Message, ct);
        if (!result.IsSuccess) return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = result.Error });
        return Ok(new { reply = result.Value });
    }
}

public class AiChatRequest
{
    public Guid? CompanyId { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<ChatTurn>? History { get; set; }
}
