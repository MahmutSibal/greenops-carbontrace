using System.Security.Claims;
using GreenOps.Application.Abstractions.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GreenOps.WebApi.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsController : ApiControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    /// <summary>POST /api/reports/generate/iso-14064 — ISO 14064 denetim raporu</summary>
    [HttpPost("generate/iso-14064")]
    public async Task<IActionResult> GenerateIso14064([FromBody] GenerateReportRequest request, CancellationToken ct)
    {
        if (!CanAccessCompany(request.CompanyId)) return Forbid();
        var result = await _reportService.GenerateIso14064Async(request.CompanyId, request.Year, ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

    /// <summary>POST /api/reports/generate/ghg-protocol — GHG Protokolü raporu</summary>
    [HttpPost("generate/ghg-protocol")]
    public async Task<IActionResult> GenerateGhgProtocol([FromBody] GenerateReportRequest request, CancellationToken ct)
    {
        if (!CanAccessCompany(request.CompanyId)) return Forbid();
        var result = await _reportService.GenerateGhgProtocolAsync(request.CompanyId, request.Year, ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

    /// <summary>GET /api/reports/export/excel — Veri tablosunu Excel olarak indirir</summary>
    [HttpGet("export/excel")]
    public async Task<IActionResult> ExportExcel([FromQuery] Guid companyId, [FromQuery] string dataType, CancellationToken ct)
    {
        if (!CanAccessCompany(companyId)) return Forbid();
        var result = await _reportService.ExportExcelAsync(companyId, dataType, ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return File(result.Value!, "text/csv", $"{dataType}_{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    /// <summary>GET /api/reports/inventory?year=2026&amp;format=pdf|xlsx — Yıllık envanter özeti (yalnızca onaylı kayıtlar)</summary>
    [HttpGet("inventory")]
    public async Task<IActionResult> Inventory([FromQuery] Guid? companyId, [FromQuery] int year, [FromQuery] string format, CancellationToken ct)
    {
        if (!TryResolveCompany(companyId, out var cid)) return CompanyForbidden();
        var result = await _reportService.ExportInventoryAsync(cid, year, format?.ToLowerInvariant() ?? "", ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return File(result.Value!.Content, result.Value.ContentType, result.Value.FileName);
    }

    /// <summary>GET /api/reports/history — Geçmiş rapor arşivi</summary>
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory([FromQuery] Guid companyId, CancellationToken ct)
    {
        if (!CanAccessCompany(companyId)) return Forbid();
        var result = await _reportService.GetHistoryAsync(companyId, ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

}

public class GenerateReportRequest { public Guid CompanyId { get; set; } public int Year { get; set; } }
