using GreenOps.Application.Abstractions.Repositories;
using GreenOps.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GreenOps.WebApi.Controllers;

[Route("api/facilities")]
[Authorize]
public class FacilitiesController : ApiControllerBase
{
    private readonly IFacilityRepository _facilityRepository;

    public FacilitiesController(IFacilityRepository facilityRepository)
    {
        _facilityRepository = facilityRepository;
    }

    /// <summary>GET /api/facilities — Aktif tesisleri listeler (SuperAdmin companyId vermezse hepsi)</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid? companyId, CancellationToken ct)
    {
        IReadOnlyList<Facility> facilities;
        if (IsSuperAdmin && (companyId is null || companyId == Guid.Empty))
        {
            facilities = await _facilityRepository.ListAsync(f => f.IsActive, ct);
        }
        else
        {
            if (!TryResolveCompany(companyId, out var cid)) return CompanyForbidden();
            facilities = await _facilityRepository.ListByCompanyAsync(cid, ct);
        }

        return Ok(facilities.Select(ToDto));
    }

    /// <summary>POST /api/facilities — Yeni tesis ekler (SuperAdmin, CompanyAdmin, Manager)</summary>
    [HttpPost]
    [Authorize(Roles = ManagerRoles)]
    public async Task<IActionResult> Create([FromBody] CreateFacilityRequest request, CancellationToken ct)
    {
        if (!TryResolveCompany(request.CompanyId, out var cid)) return CompanyForbidden();

        var error = Validate(request.Name, request.Address, request.Department, request.SquareMeters, request.EmployeeCount);
        if (error is not null) return BadRequest(new { error });

        var name = request.Name.Trim();
        if ((await _facilityRepository.ListByCompanyAsync(cid, ct)).Any(f => f.Name == name))
            return Conflict(new { error = "Bu adla aktif bir tesis zaten var." });

        var facility = new Facility
        {
            Id = Guid.NewGuid(),
            CompanyId = cid,
            Name = name,
            Address = request.Address,
            SquareMeters = request.SquareMeters,
            EmployeeCount = request.EmployeeCount,
            Department = request.Department,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _facilityRepository.AddAsync(facility, ct);
        await _facilityRepository.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = facility.Id }, new { facility.Id });
    }

    /// <summary>GET /api/facilities/{id} — Tek bir tesisin detayları</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var f = await FindAccessibleAsync(id, ct);
        if (f is null) return NotFoundError("Tesis bulunamadı.");
        return Ok(ToDto(f));
    }

    /// <summary>PUT /api/facilities/{id} — Tesis bilgilerini günceller (SuperAdmin, CompanyAdmin, Manager)</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = ManagerRoles)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateFacilityRequest request, CancellationToken ct)
    {
        var facility = await FindAccessibleAsync(id, ct);
        if (facility is null || !facility.IsActive) return NotFoundError("Tesis bulunamadı.");

        var error = Validate(request.Name ?? facility.Name, request.Address, request.Department, request.SquareMeters, request.EmployeeCount);
        if (error is not null) return BadRequest(new { error });

        facility.Name = request.Name?.Trim() ?? facility.Name;
        facility.Address = request.Address ?? facility.Address;
        facility.SquareMeters = request.SquareMeters ?? facility.SquareMeters;
        facility.EmployeeCount = request.EmployeeCount ?? facility.EmployeeCount;
        facility.Department = request.Department ?? facility.Department;

        _facilityRepository.Update(facility);
        await _facilityRepository.SaveChangesAsync(ct);
        return Ok(new { message = "Tesis güncellendi." });
    }

    /// <summary>DELETE /api/facilities/{id} — Tesisi pasife alır (emisyon geçmişi korunur)</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = AdminRoles)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var facility = await FindAccessibleAsync(id, ct);
        if (facility is null || !facility.IsActive) return NotFoundError("Tesis bulunamadı.");

        // FK'lar Restrict: hard delete emisyon kaydı olan tesiste hata verir ve geçmişi yok ederdi.
        facility.IsActive = false;
        _facilityRepository.Update(facility);
        await _facilityRepository.SaveChangesAsync(ct);
        return Ok(new { message = "Tesis silindi." });
    }

    private async Task<Facility?> FindAccessibleAsync(Guid id, CancellationToken ct)
    {
        var facility = await _facilityRepository.GetByIdAsync(id, ct);
        return facility is not null && CanAccessCompany(facility.CompanyId) ? facility : null;
    }

    private static object ToDto(Facility f) => new
    {
        f.Id, f.CompanyId, f.Name, f.Address, f.SquareMeters, f.EmployeeCount, f.Department, f.IsActive, f.CreatedAt
    };

    private static string? Validate(string? name, string? address, string? department, decimal? squareMeters, int? employees)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 200) return "Tesis adı 1-200 karakter olmalıdır.";
        if (address?.Length > 500) return "Adres en fazla 500 karakter olabilir.";
        if (department?.Length > 200) return "Departman en fazla 200 karakter olabilir.";
        if (squareMeters < 0) return "Alan negatif olamaz.";
        if (employees < 0) return "Çalışan sayısı negatif olamaz.";
        return null;
    }
}

public class CreateFacilityRequest
{
    public Guid? CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public decimal? SquareMeters { get; set; }
    public int? EmployeeCount { get; set; }
    public string? Department { get; set; }
}

public class UpdateFacilityRequest
{
    public string? Name { get; set; }
    public string? Address { get; set; }
    public decimal? SquareMeters { get; set; }
    public int? EmployeeCount { get; set; }
    public string? Department { get; set; }
}
