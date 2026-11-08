using GreenOps.Application.Abstractions.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GreenOps.WebApi.Controllers;

[ApiController]
[Route("api/carbon-passports")]
[Authorize(Roles = ApiControllerBase.ManagerRoles)]
public class ExportCarbonPassportsController : ApiControllerBase
{
    private readonly IExportCarbonPassportService _service;

    public ExportCarbonPassportsController(IExportCarbonPassportService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromQuery] Guid? companyId,
        [FromBody] CreateExportCarbonPassportRequest request,
        CancellationToken ct)
    {
        if (!TryResolveCompany(companyId, out var cid)) return CompanyForbidden();
        var result = await _service.CreateAsync(cid, request, ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return CreatedAtAction(nameof(Get), new { id = result.Value!.Id }, result.Value);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, [FromQuery] Guid? companyId, CancellationToken ct)
    {
        if (!TryResolveCompany(companyId, out var cid)) return CompanyForbidden();
        var result = await _service.GetAsync(cid, id, ct);
        if (!result.IsSuccess) return NotFound(new { error = result.Error });
        return Ok(result.Value);
    }

    [HttpGet("{id:guid}/pdf")]
    public async Task<IActionResult> Pdf(Guid id, [FromQuery] Guid? companyId, CancellationToken ct)
    {
        if (!TryResolveCompany(companyId, out var cid)) return CompanyForbidden();
        var result = await _service.ExportPdfAsync(cid, id, ct);
        if (!result.IsSuccess) return NotFound(new { error = result.Error });
        return File(result.Value!.Content, result.Value.ContentType, result.Value.FileName);
    }

    [HttpPost("{id:guid}/revoke")]
    public async Task<IActionResult> Revoke(Guid id, [FromQuery] Guid? companyId, CancellationToken ct)
    {
        if (!TryResolveCompany(companyId, out var cid)) return CompanyForbidden();
        var result = await _service.RevokeAsync(cid, id, ct);
        if (!result.IsSuccess) return NotFound(new { error = result.Error });
        return NoContent();
    }
}

[ApiController]
[Route("api/public/carbon-passports")]
public class PublicCarbonPassportsController : ControllerBase
{
    private readonly IExportCarbonPassportService _service;

    public PublicCarbonPassportsController(IExportCarbonPassportService service)
    {
        _service = service;
    }

    [AllowAnonymous]
    [HttpGet("{token}")]
    public async Task<IActionResult> Get(string token, CancellationToken ct)
    {
        var result = await _service.GetPublicAsync(token, ct);
        if (!result.IsSuccess) return NotFound(new { error = result.Error });
        return Ok(result.Value);
    }
}
