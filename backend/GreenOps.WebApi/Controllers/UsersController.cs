using GreenOps.Application.Abstractions.Repositories;
using GreenOps.Application.Abstractions.Services;
using GreenOps.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GreenOps.WebApi.Controllers;

[Route("api/users")]
[Authorize]
public class UsersController : ApiControllerBase
{
    private readonly IUserService _userService;
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IFileStorage _fileStorage;

    public UsersController(
        IUserService userService,
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IFileStorage fileStorage)
    {
        _userService = userService;
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _fileStorage = fileStorage;
    }

    /// <summary>GET /api/users/me — Giriş yapan kullanıcının profil bilgileri</summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetMe(CancellationToken ct)
    {
        if (CurrentUserId is not { } userId) return Unauthorized();
        var result = await _userService.GetProfileAsync(userId, ct);
        if (!result.IsSuccess) return NotFound(new { error = result.Error });
        return Ok(result.Value);
    }

    /// <summary>PUT /api/users/me — Kendi profil bilgilerini günceller</summary>
    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateProfileRequest request, CancellationToken ct)
    {
        if (CurrentUserId is not { } userId) return Unauthorized();
        var result = await _userService.UpdateProfileAsync(userId, request, ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(new { message = "Profil güncellendi." });
    }

    /// <summary>PUT /api/users/me/change-password — Mevcut şifreyi değiştirir (diğer oturumlar kapanır)</summary>
    [HttpPut("me/change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        if (CurrentUserId is not { } userId) return Unauthorized();
        var result = await _userService.ChangePasswordAsync(userId, request.CurrentPassword, request.NewPassword, ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(new { message = "Şifre değiştirildi." });
    }

    /// <summary>POST /api/users/me/avatar — Profil fotoğrafı yükler (png/jpg/webp, en fazla 2 MB)</summary>
    [HttpPost("me/avatar")]
    [RequestSizeLimit(3 * 1024 * 1024)]
    public async Task<IActionResult> UploadAvatar(IFormFile file, CancellationToken ct)
    {
        if (CurrentUserId is not { } userId) return Unauthorized();
        if (file is null || file.Length == 0) return BadRequest(new { error = "Dosya seçilmedi." });
        await using var stream = file.OpenReadStream();
        var result = await _userService.UploadAvatarAsync(userId, file.FileName, file.Length, stream, ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(new { avatarUrl = result.Value });
    }

    /// <summary>GET /api/users/me/avatar — Profil fotoğrafını döner</summary>
    [HttpGet("me/avatar")]
    public async Task<IActionResult> GetAvatar(CancellationToken ct)
    {
        if (CurrentUserId is not { } userId) return Unauthorized();
        var user = await _userRepository.GetByIdAsync(userId, ct);
        var path = user?.AvatarUrl is { } stored ? _fileStorage.ResolvePath(stored) : null;
        if (path is null) return NotFoundError("Profil fotoğrafı yok.");

        var contentType = Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "image/jpeg"
        };
        return PhysicalFile(path, contentType);
    }

    /// <summary>GET /api/users — Şirketteki personeller (SuperAdmin, CompanyAdmin, Manager)</summary>
    [HttpGet]
    [Authorize(Roles = ManagerRoles)]
    public async Task<IActionResult> ListUsers([FromQuery] Guid? companyId, CancellationToken ct)
    {
        if (!TryResolveCompany(companyId, out var cid)) return CompanyForbidden();
        var result = await _userService.ListByCompanyAsync(cid, ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

    /// <summary>POST /api/users/invite — Çalışanı davet eder (SuperAdmin, CompanyAdmin)</summary>
    [HttpPost("invite")]
    [Authorize(Roles = AdminRoles)]
    public async Task<IActionResult> InviteUser([FromBody] InviteUserRequest request, CancellationToken ct)
    {
        if (!TryResolveCompany(request.CompanyId, out var cid)) return CompanyForbidden();
        if (!Enum.TryParse<UserRole>(request.Role, true, out var role))
            return BadRequest(new { error = "Geçersiz rol. Geçerli roller: CompanyAdmin, Manager, Personnel" });
        if (!CanGrant(role)) return Forbid();

        request.CompanyId = cid;
        request.Role = role.ToString();
        var result = await _userService.InviteUserAsync(request, ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(new { message = "Kullanıcı oluşturuldu." });
    }

    /// <summary>PUT /api/users/{id}/status — Hesabı dondur/aktif et (SuperAdmin, CompanyAdmin)</summary>
    [HttpPut("{id:guid}/status")]
    [Authorize(Roles = AdminRoles)]
    public async Task<IActionResult> ChangeUserStatus(Guid id, [FromBody] ChangeStatusRequest request, CancellationToken ct)
    {
        var target = await _userRepository.GetByIdAsync(id, ct);
        var denied = CheckManageable(target);
        if (denied is not null) return denied;

        var result = await _userService.ChangeUserStatusAsync(id, request.IsActive, ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(new { message = "Kullanıcı durumu güncellendi." });
    }

    /// <summary>PUT /api/users/{id}/roles — Kullanıcıya özel rol ata (SuperAdmin, CompanyAdmin)</summary>
    [HttpPut("{id:guid}/roles")]
    [Authorize(Roles = AdminRoles)]
    public async Task<IActionResult> AssignRole(Guid id, [FromBody] AssignRoleRequest request, CancellationToken ct)
    {
        var target = await _userRepository.GetByIdAsync(id, ct);
        var denied = CheckManageable(target);
        if (denied is not null) return denied;

        var result = await _userService.AssignRoleAsync(id, request.RoleId, ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(new { message = "Rol atandı." });
    }

    /// <summary>PUT /api/users/{id}/set-role — Sistem rolü atar (SuperAdmin her rolü, CompanyAdmin Manager/Personnel)</summary>
    [HttpPut("{id:guid}/set-role")]
    [Authorize(Roles = AdminRoles)]
    public async Task<IActionResult> SetUserRole(Guid id, [FromBody] SetUserRoleRequest request, CancellationToken ct)
    {
        if (!Enum.TryParse<UserRole>(request.Role, true, out var targetRole))
            return BadRequest(new { error = "Geçersiz rol adı. Geçerli roller: SuperAdmin, CompanyAdmin, Manager, Personnel" });
        if (!CanGrant(targetRole)) return Forbid();

        var targetUser = await _userRepository.GetByIdAsync(id, ct);
        var denied = CheckManageable(targetUser);
        if (denied is not null) return denied;

        targetUser!.Role = targetRole;
        _userRepository.Update(targetUser);
        // Rol değişince açık oturumlar kapanır; erişim token'ı da bir sonraki istekte reddedilir.
        await _refreshTokenRepository.RevokeAllForUserAsync(id, ct);
        await _userRepository.SaveChangesAsync(ct);

        return Ok(new { message = $"Kullanıcı rolü '{targetRole}' olarak güncellendi." });
    }

    /// <summary>CompanyAdmin en fazla Manager/Personnel atayabilir; SuperAdmin her rolü.</summary>
    private bool CanGrant(UserRole role) =>
        IsSuperAdmin || (CurrentRole == UserRole.CompanyAdmin && role > UserRole.CompanyAdmin);

    /// <summary>
    /// Hedef kullanıcı yönetilebilir mi: kendisi olamaz, başka şirkette olamaz,
    /// CompanyAdmin kendisiyle aynı veya üst rütbedeki birini değiştiremez.
    /// </summary>
    private IActionResult? CheckManageable(AppUser? target)
    {
        if (target is null || (!IsSuperAdmin && target.CompanyId != CurrentCompanyId))
            return NotFoundError("Kullanıcı bulunamadı.");
        if (target.Id == CurrentUserId)
            return BadRequest(new { error = "Kendi hesabınızın rolünü veya durumunu değiştiremezsiniz." });
        if (!IsSuperAdmin && target.Role <= CurrentRole)
            return Forbid();
        return null;
    }
}

public class ChangePasswordRequest { public string CurrentPassword { get; set; } = string.Empty; public string NewPassword { get; set; } = string.Empty; }
public class ChangeStatusRequest { public bool IsActive { get; set; } }
public class AssignRoleRequest { public Guid RoleId { get; set; } }
public class SetUserRoleRequest { public string Role { get; set; } = string.Empty; }
