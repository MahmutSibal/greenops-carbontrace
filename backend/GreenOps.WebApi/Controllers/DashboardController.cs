using System.Security.Claims;
using GreenOps.Application.Abstractions.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GreenOps.WebApi.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController : ApiControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    /// <summary>GET /api/dashboard/kpi — Toplam Salınım, Scope Dağılımı, Aylık Trendler özet</summary>
    [HttpGet("kpi")]
    public async Task<IActionResult> GetKpi([FromQuery] Guid companyId, CancellationToken ct)
    {
        if (!CanAccessCompany(companyId)) return Forbid();
        var result = await _dashboardService.GetSummaryAsync(companyId, ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

    /// <summary>GET /api/dashboard/facility-comparison — Tesisler arası verimlilik kıyaslama</summary>
    [HttpGet("facility-comparison")]
    public async Task<IActionResult> GetFacilityComparison([FromQuery] Guid companyId, CancellationToken ct)
    {
        if (!CanAccessCompany(companyId)) return Forbid();
        var result = await _dashboardService.GetFacilityComparisonAsync(companyId, ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

    /// <summary>GET /api/dashboard/intensity — Karbon Yoğunluğu (çalışan/ciro başına)</summary>
    [HttpGet("intensity")]
    public async Task<IActionResult> GetIntensity([FromQuery] Guid companyId, CancellationToken ct)
    {
        if (!CanAccessCompany(companyId)) return Forbid();
        var result = await _dashboardService.GetSummaryAsync(companyId, ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });

        var summary = result.Value!;
        return Ok(new
        {
            companyId,
            perEmployee = summary.CarbonIntensity,
            totalEmissions = summary.TotalEmissions,
            note = "Çalışan başına karbon yoğunluğu hesaplandı."
        });
    }

    /// <summary>GET /api/dashboard/trends — Emisyon trend verileri</summary>
    [HttpGet("trends")]
    public async Task<IActionResult> GetTrends([FromQuery] Guid companyId, [FromQuery] int? year, CancellationToken ct)
    {
        if (!CanAccessCompany(companyId)) return Forbid();
        var result = await _dashboardService.GetTrendsAsync(companyId, year, ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

}

[ApiController]
[Route("api/analytics")]
[Authorize]
public class AnalyticsController : ApiControllerBase
{
    private readonly IAiService _aiService;

    public AnalyticsController(IAiService aiService)
    {
        _aiService = aiService;
    }

    /// <summary>GET /api/analytics/ai-prediction — Gelecek ay tahmini ve AI önerileri (/api/ai/insights ile aynı)</summary>
    [HttpGet("ai-prediction")]
    [EnableRateLimiting(RateLimitPolicies.Ai)]
    public async Task<IActionResult> GetAiPrediction([FromQuery] Guid? companyId, CancellationToken ct)
    {
        if (!TryResolveCompany(companyId, out var cid)) return CompanyForbidden();
        var result = await _aiService.GetInsightsAsync(cid, refresh: false, ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }
}
