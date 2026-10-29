using GreenOps.Application.Abstractions.Repositories;
using GreenOps.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GreenOps.WebApi.Controllers;

[Route("api/companies")]
[Authorize]
public class CompaniesController : ApiControllerBase
{
    private readonly ICompanyRepository _companyRepository;
    private readonly IFacilityRepository _facilityRepository;

    public CompaniesController(ICompanyRepository companyRepository, IFacilityRepository facilityRepository)
    {
        _companyRepository = companyRepository;
        _facilityRepository = facilityRepository;
    }

    /// <summary>POST /api/companies — Yeni şirket oluşturur (SuperAdmin; diğerleri kayıt ile şirket açar)</summary>
    [HttpPost]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateCompanyRequest request, CancellationToken ct)
    {
        var error = Validate(request.Name, request.TaxNo, request.Address, request.Industry, request.EmployeeCount, request.AnnualRevenue);
        if (error is not null) return BadRequest(new { error });

        var company = new Company
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            TaxNo = request.TaxNo?.Trim() ?? "",
            Address = request.Address,
            Industry = request.Industry,
            EmployeeCount = request.EmployeeCount,
            AnnualRevenue = request.AnnualRevenue,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _companyRepository.AddAsync(company, ct);
        await _companyRepository.SaveChangesAsync(ct);

        return Ok(ToDto(company));
    }

    /// <summary>GET /api/companies — SuperAdmin tüm şirketleri, diğerleri sadece kendi şirketini görür</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        if (IsSuperAdmin)
        {
            var companies = await _companyRepository.ListAsync(ct);
            return Ok(companies.Select(ToDto));
        }

        if (CurrentCompanyId is not { } own) return Ok(Array.Empty<object>());
        var mine = await _companyRepository.GetByIdAsync(own, ct);
        return Ok(mine is null ? Array.Empty<object>() : new[] { ToDto(mine) });
    }

    /// <summary>GET /api/companies/my-company — Kendi şirket bilgileri ve abonelik</summary>
    [HttpGet("my-company")]
    public async Task<IActionResult> GetMyCompany([FromQuery] Guid? companyId, CancellationToken ct)
    {
        if (!TryResolveCompany(companyId, out var cid)) return CompanyForbidden();
        var company = await _companyRepository.GetByIdAsync(cid, ct);
        if (company is null) return NotFoundError("Şirket bulunamadı.");

        return Ok(new
        {
            company.Id, company.Name, company.TaxNo, company.Address, company.Industry,
            company.EmployeeCount, company.AnnualRevenue, company.BillingPlanId, company.IsActive, company.CreatedAt
        });
    }

    /// <summary>PUT /api/companies/my-company — Şirket detaylarını günceller (SuperAdmin, CompanyAdmin)</summary>
    [HttpPut("my-company")]
    [Authorize(Roles = AdminRoles)]
    public async Task<IActionResult> UpdateMyCompany([FromQuery] Guid? companyId, [FromBody] UpdateCompanyRequest request, CancellationToken ct)
    {
        if (!TryResolveCompany(companyId, out var cid)) return CompanyForbidden();
        var company = await _companyRepository.GetByIdAsync(cid, ct);
        if (company is null) return NotFoundError("Şirket bulunamadı.");

        var error = Validate(request.Name ?? company.Name, request.TaxNo, request.Address, request.Industry, request.EmployeeCount, request.AnnualRevenue);
        if (error is not null) return BadRequest(new { error });

        company.Name = request.Name?.Trim() ?? company.Name;
        company.TaxNo = request.TaxNo?.Trim() ?? company.TaxNo;
        company.Address = request.Address ?? company.Address;
        company.Industry = request.Industry ?? company.Industry;
        company.EmployeeCount = request.EmployeeCount ?? company.EmployeeCount;
        company.AnnualRevenue = request.AnnualRevenue ?? company.AnnualRevenue;

        _companyRepository.Update(company);
        await _companyRepository.SaveChangesAsync(ct);

        return Ok(new { message = "Şirket bilgileri güncellendi." });
    }

    /// <summary>GET /api/companies/hierarchy — Departman ve tesis ağacı</summary>
    [HttpGet("hierarchy")]
    public async Task<IActionResult> GetHierarchy([FromQuery] Guid? companyId, CancellationToken ct)
    {
        if (IsSuperAdmin && (companyId is null || companyId == Guid.Empty))
            return Ok(new { id = (Guid?)null, name = "Tüm Şirketler", facilities = Array.Empty<object>() });

        if (!TryResolveCompany(companyId, out var cid)) return CompanyForbidden();
        var company = await _companyRepository.GetByIdAsync(cid, ct);
        if (company is null) return NotFoundError("Şirket bulunamadı.");

        var facilities = await _facilityRepository.ListByCompanyAsync(cid, ct);

        return Ok(new
        {
            company.Id,
            company.Name,
            Facilities = facilities.Select(f => new { f.Id, f.Name, f.Address, f.Department, f.IsActive })
        });
    }

    private static object ToDto(Company c) => new
    {
        c.Id, c.Name, c.TaxNo, c.Address, c.Industry, c.EmployeeCount, c.AnnualRevenue, c.IsActive, c.CreatedAt
    };

    private static string? Validate(string? name, string? taxNo, string? address, string? industry, int? employees, decimal? revenue)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 200) return "Şirket adı 1-200 karakter olmalıdır.";
        if (taxNo?.Length > 50) return "Vergi numarası en fazla 50 karakter olabilir.";
        if (address?.Length > 500) return "Adres en fazla 500 karakter olabilir.";
        if (industry?.Length > 100) return "Sektör en fazla 100 karakter olabilir.";
        if (employees < 0) return "Çalışan sayısı negatif olamaz.";
        if (revenue < 0) return "Ciro negatif olamaz.";
        return null;
    }
}

public class CreateCompanyRequest
{
    public string Name { get; set; } = string.Empty;
    public string? TaxNo { get; set; }
    public string? Address { get; set; }
    public string? Industry { get; set; }
    public int? EmployeeCount { get; set; }
    public decimal? AnnualRevenue { get; set; }
}

public class UpdateCompanyRequest
{
    public string? Name { get; set; }
    public string? TaxNo { get; set; }
    public string? Address { get; set; }
    public string? Industry { get; set; }
    public int? EmployeeCount { get; set; }
    public decimal? AnnualRevenue { get; set; }
}
