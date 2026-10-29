using GreenOps.Application.Abstractions.Repositories;
using GreenOps.Application.Abstractions.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GreenOps.WebApi.Controllers;

[Route("api/suppliers")]
[Authorize(Roles = ManagerRoles)]
public class SuppliersController : ApiControllerBase
{
    private readonly ISupplierService _supplierService;
    private readonly ISupplierRepository _supplierRepository;

    public SuppliersController(ISupplierService supplierService, ISupplierRepository supplierRepository)
    {
        _supplierService = supplierService;
        _supplierRepository = supplierRepository;
    }

    /// <summary>GET /api/suppliers — Şirketin tedarikçilerini listeler</summary>
    [HttpGet]
    public async Task<IActionResult> ListSuppliers([FromQuery] Guid? companyId, CancellationToken ct)
    {
        if (!TryResolveCompany(companyId, out var cid)) return CompanyForbidden();
        var result = await _supplierService.ListByCompanyAsync(cid, ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

    /// <summary>POST /api/suppliers/invite — Tedarikçi ekler (davet e-postası henüz yok)</summary>
    [HttpPost("invite")]
    public async Task<IActionResult> InviteSupplier([FromBody] InviteSupplierRequest request, CancellationToken ct)
    {
        if (!TryResolveCompany(request.CompanyId, out var cid)) return CompanyForbidden();
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 200)
            return BadRequest(new { error = "Tedarikçi adı 1-200 karakter olmalıdır." });
        if (string.IsNullOrWhiteSpace(request.ContactEmail) || request.ContactEmail.Length > 200 || !request.ContactEmail.Contains('@'))
            return BadRequest(new { error = "Geçerli bir iletişim e-postası giriniz." });

        request.CompanyId = cid;
        var result = await _supplierService.InviteSupplierAsync(request, ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(new { message = "Tedarikçi eklendi." });
    }

    /// <summary>GET /api/suppliers/{id}/emissions — Tedarikçi emisyonları</summary>
    [HttpGet("{id:guid}/emissions")]
    public async Task<IActionResult> GetSupplierEmissions(Guid id, CancellationToken ct)
    {
        if (!await CanAccessSupplierAsync(id, ct)) return NotFoundError("Tedarikçi bulunamadı.");
        var result = await _supplierService.GetSupplierEmissionsAsync(id, ct);
        if (!result.IsSuccess) return NotFound(new { error = result.Error });
        return Ok(result.Value);
    }

    /// <summary>PUT /api/suppliers/{id}/esg-score — ESG puanını günceller</summary>
    [HttpPut("{id:guid}/esg-score")]
    public async Task<IActionResult> UpdateESGScore(Guid id, [FromBody] UpdateESGScoreRequest request, CancellationToken ct)
    {
        if (!await CanAccessSupplierAsync(id, ct)) return NotFoundError("Tedarikçi bulunamadı.");
        var result = await _supplierService.UpdateESGScoreAsync(id, request.Score, ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(new { message = "ESG puanı güncellendi." });
    }

    private async Task<bool> CanAccessSupplierAsync(Guid id, CancellationToken ct)
    {
        var supplier = await _supplierRepository.GetByIdAsync(id, ct);
        return supplier is not null && CanAccessCompany(supplier.CompanyId);
    }
}

public class UpdateESGScoreRequest { public int Score { get; set; } }
