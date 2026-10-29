using GreenOps.Application.Abstractions.Repositories;
using GreenOps.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GreenOps.WebApi.Controllers;

[ApiController]
[Route("api/roles")]
[Authorize(Roles = "SuperAdmin,CompanyAdmin")]
public class RolesController : ControllerBase
{
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;

    public RolesController(IRoleRepository roleRepository, IPermissionRepository permissionRepository)
    {
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
    }

    /// <summary>GET /api/roles — Tüm rolleri listeler</summary>
    [HttpGet]
    public async Task<IActionResult> GetRoles(CancellationToken ct)
    {
        var roles = await _roleRepository.ListAsync(ct);
        return Ok(roles.Select(r => new { r.Id, r.Name, r.IsSystem, r.CompanyId, r.CreatedAt }));
    }

    /// <summary>POST /api/roles — Dinamik rol oluşturma devre dışı bırakıldı</summary>
    [HttpPost]
    public IActionResult CreateRole([FromBody] CreateRoleRequest request)
    {
        return BadRequest(new { error = "Dinamik rol oluşturma devre dışıdır. Sistem rolleri: SuperAdmin, CompanyAdmin, Manager, Personnel." });
    }
}

[ApiController]
[Route("api/permissions")]
[Authorize]
public class PermissionsController : ControllerBase
{
    private readonly IPermissionRepository _permissionRepository;

    public PermissionsController(IPermissionRepository permissionRepository)
    {
        _permissionRepository = permissionRepository;
    }

    /// <summary>GET /api/permissions — Tüm yetki tanımlarını döner</summary>
    [HttpGet]
    public async Task<IActionResult> GetPermissions(CancellationToken ct)
    {
        var permissions = await _permissionRepository.ListAsync(ct);
        return Ok(permissions.Select(p => new { p.Id, p.Name, p.Description }));
    }
}

public class CreateRoleRequest { public string Name { get; set; } = string.Empty; public Guid? CompanyId { get; set; } }
