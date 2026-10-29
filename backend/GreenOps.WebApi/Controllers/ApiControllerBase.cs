using System.Security.Claims;
using GreenOps.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace GreenOps.WebApi.Controllers;

/// <summary>
/// Kiracı (şirket) izolasyonu için ortak yardımcılar. Şirket kimliği istemciden değil
/// JWT'deki companyId claim'inden gelir; sadece SuperAdmin başka şirket seçebilir.
/// </summary>
[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    public const string AdminRoles = "SuperAdmin,CompanyAdmin";
    public const string ManagerRoles = "SuperAdmin,CompanyAdmin,Manager";

    protected Guid? CurrentUserId =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    protected Guid? CurrentCompanyId =>
        Guid.TryParse(User.FindFirstValue("companyId"), out var id) ? id : null;

    protected UserRole CurrentRole =>
        Enum.TryParse<UserRole>(User.FindFirstValue(ClaimTypes.Role), out var role) ? role : UserRole.Personnel;

    protected bool IsSuperAdmin => CurrentRole == UserRole.SuperAdmin;

    protected bool IsAtLeast(UserRole role) => CurrentRole <= role; // Enum: SuperAdmin=0 … Personnel=3

    protected bool CanAccessCompany(Guid companyId) =>
        IsSuperAdmin || (CurrentCompanyId.HasValue && CurrentCompanyId.Value == companyId);

    /// <summary>
    /// İşlem yapılacak şirketi belirler. SuperAdmin için istekteki companyId zorunludur;
    /// diğer roller için kendi şirketidir ve farklı bir companyId gönderilirse reddedilir.
    /// </summary>
    protected bool TryResolveCompany(Guid? requested, out Guid companyId)
    {
        companyId = Guid.Empty;
        var hasRequest = requested.HasValue && requested.Value != Guid.Empty;

        if (IsSuperAdmin)
        {
            if (!hasRequest) return false;
            companyId = requested!.Value;
            return true;
        }

        if (CurrentCompanyId is not { } own) return false;
        if (hasRequest && requested!.Value != own) return false;

        companyId = own;
        return true;
    }

    /// <summary>SuperAdmin için null (tüm şirketler), diğerleri için kendi şirketi.</summary>
    protected Guid? CompanyScope => IsSuperAdmin ? null : CurrentCompanyId ?? Guid.Empty;

    protected IActionResult CompanyForbidden() =>
        IsSuperAdmin
            ? BadRequest(new { error = "SuperAdmin için companyId parametresi zorunludur." })
            : Forbid();

    /// <summary>Kaynak yoksa ya da başka şirkete aitse aynı yanıt döner (varlık bilgisi sızdırılmaz).</summary>
    protected IActionResult NotFoundError(string message) => NotFound(new { error = message });
}
