using GreenOps.Application.Abstractions.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GreenOps.WebApi.Controllers;

[ApiController]
[Route("api/billing")]
[Authorize]
public class BillingController : ApiControllerBase
{
    private readonly IBillingService _billingService;

    public BillingController(IBillingService billingService)
    {
        _billingService = billingService;
    }

    /// <summary>GET /api/billing/plan — Mevcut abonelik paketini döner</summary>
    [HttpGet("plan")]
    public async Task<IActionResult> GetPlan([FromQuery] Guid companyId, CancellationToken ct)
    {
        if (!CanAccessCompany(companyId)) return Forbid();
        var result = await _billingService.GetCurrentPlanAsync(companyId, ct);
        if (!result.IsSuccess) return NotFound(new { error = result.Error });
        return Ok(result.Value);
    }

    /// <summary>POST /api/billing/upgrade — Üst pakete geçiş</summary>
    [HttpPost("upgrade")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> UpgradePlan([FromQuery] Guid companyId, [FromBody] UpgradePlanRequest request, CancellationToken ct)
    {
        var result = await _billingService.UpgradePlanAsync(companyId, request, ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(new { message = "Paket yükseltildi." });
    }

    /// <summary>GET /api/billing/invoices — Aylık faturaları listeler</summary>
    [HttpGet("invoices")]
    public async Task<IActionResult> ListInvoices([FromQuery] Guid companyId, CancellationToken ct)
    {
        if (!CanAccessCompany(companyId)) return Forbid();
        var result = await _billingService.ListInvoicesAsync(companyId, ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

}
