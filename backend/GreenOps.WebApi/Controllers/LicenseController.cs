using GreenOps.Application.Abstractions.Repositories;
using GreenOps.Application.Abstractions.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GreenOps.WebApi.Controllers;

[ApiController]
[Route("api/license")]
[Authorize]
public class LicenseController : ApiControllerBase
{
    private readonly ILicenseService _licenseService;
    private readonly ILicenseRepository _licenseRepository;

    public LicenseController(ILicenseService licenseService, ILicenseRepository licenseRepository)
    {
        _licenseService = licenseService;
        _licenseRepository = licenseRepository;
    }

    /// <summary>POST /api/license/generate — Admin lisans anahtarı oluşturma</summary>
    [HttpPost("generate")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Generate([FromBody] CreateLicenseRequest request, CancellationToken ct)
    {
        // SuperAdmin kendi şirketi için lisans üretmesin.
        var userCompanyClaim = User.FindFirstValue("companyId");
        if (!string.IsNullOrWhiteSpace(userCompanyClaim)
            && Guid.TryParse(userCompanyClaim, out var userCompanyId)
            && userCompanyId == request.CompanyId)
        {
            return BadRequest(new { error = "SuperAdmin kendi şirketine lisans tanımlayamaz." });
        }

        var result = await _licenseService.CreateAsync(request, ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

    /// <summary>POST /api/license/activate — Cihaz lisans kilitleme</summary>
    [HttpPost("activate")]
    [Authorize(Roles = AdminRoles)]
    public async Task<IActionResult> Activate([FromBody] ActivateLicenseRequest request, CancellationToken ct)
    {
        // Anahtar başka bir şirkete aitse aktive edilemez (varlığı da sızdırılmaz).
        var license = await _licenseRepository.GetByKeyAsync(request.LicenseKey ?? string.Empty, ct);
        if (license is null || !CanAccessCompany(license.CompanyId))
            return BadRequest(new { error = "Lisans anahtarı bulunamadı." });

        var result = await _licenseService.ActivateAsync(request, ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(new { message = "Lisans başarıyla aktif edildi." });
    }

    /// <summary>GET /api/license/status — Lisans durumu</summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetStatus([FromQuery] Guid companyId, CancellationToken ct)
    {
        if (!CanAccessCompany(companyId)) return Forbid();

        var result = await _licenseService.GetStatusAsync(companyId, ct);
        if (!result.IsSuccess) return NotFound(new { error = result.Error });

        var isSuperAdmin = User.IsInRole("SuperAdmin");
        if (!isSuperAdmin)
        {
            result.Value!.LicenseKey = string.Empty;
        }

        return Ok(result.Value);
    }

    /// <summary>GET /api/license/list — Şirketin tüm lisansları</summary>
    [HttpGet("list")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> List([FromQuery] Guid companyId, CancellationToken ct)
    {
        if (!CanAccessCompany(companyId)) return Forbid();

        var result = await _licenseService.GetAllByCompanyAsync(companyId, ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

}
