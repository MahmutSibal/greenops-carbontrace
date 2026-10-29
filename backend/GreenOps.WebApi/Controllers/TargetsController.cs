using GreenOps.Application.Abstractions.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GreenOps.WebApi.Controllers;

[Route("api/targets")]
[Authorize]
public class TargetsController : ApiControllerBase
{
    private readonly ITargetService _targetService;

    public TargetsController(ITargetService targetService)
    {
        _targetService = targetService;
    }

    /// <summary>GET /api/targets — Karbon azaltım hedeflerini listeler</summary>
    [HttpGet]
    public async Task<IActionResult> ListTargets([FromQuery] Guid? companyId, CancellationToken ct)
    {
        if (!TryResolveCompany(companyId, out var cid)) return CompanyForbidden();
        var result = await _targetService.ListByCompanyAsync(cid, ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

    /// <summary>POST /api/targets — Yeni hedef belirler (SuperAdmin, CompanyAdmin)</summary>
    [HttpPost]
    [Authorize(Roles = AdminRoles)]
    public async Task<IActionResult> CreateTarget([FromBody] CreateTargetRequest request, CancellationToken ct)
    {
        if (!TryResolveCompany(request.CompanyId, out var cid)) return CompanyForbidden();
        request.CompanyId = cid;
        var result = await _targetService.CreateTargetAsync(request, ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Created($"/api/targets/{result.Value}", new { id = result.Value });
    }

    /// <summary>GET /api/targets/progress — Hedefe yaklaşma yüzdesi (bu yılın emisyonuna göre)</summary>
    [HttpGet("progress")]
    public async Task<IActionResult> GetProgress([FromQuery] Guid? companyId, CancellationToken ct)
    {
        if (!TryResolveCompany(companyId, out var cid)) return CompanyForbidden();
        var result = await _targetService.GetProgressAsync(cid, ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }
}

[Route("api/carbon-credits")]
[Authorize(Roles = AdminRoles)]
public class CarbonCreditsController : ApiControllerBase
{
    private readonly ITargetService _targetService;

    public CarbonCreditsController(ITargetService targetService)
    {
        _targetService = targetService;
    }

    /// <summary>POST /api/carbon-credits/purchase — Satın alınmış karbon kredisini (tCO2e) kaydeder</summary>
    [HttpPost("purchase")]
    public async Task<IActionResult> Purchase([FromBody] PurchaseCreditRequest request, CancellationToken ct)
    {
        if (!TryResolveCompany(request.CompanyId, out var cid)) return CompanyForbidden();
        if (request.Provider?.Length > 200 || request.CertificateUrl?.Length > 500)
            return BadRequest(new { error = "Sağlayıcı en fazla 200, sertifika URL'si en fazla 500 karakter olabilir." });

        request.CompanyId = cid;
        var result = await _targetService.PurchaseCarbonCreditAsync(request, ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(new { message = "Karbon kredisi sisteme işlendi." });
    }
}
