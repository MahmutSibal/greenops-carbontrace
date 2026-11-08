using GreenOps.Application.Common;

namespace GreenOps.Application.Abstractions.Services;

public interface IUserService
{
    Task<Result<UserProfileResponse>> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Result> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken = default);
    Task<Result> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default);
    Task<Result<string>> UploadAvatarAsync(Guid userId, string fileName, long length, Stream fileStream, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<UserListItem>>> ListByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<Result<InviteUserResponse>> InviteUserAsync(InviteUserRequest request, CancellationToken cancellationToken = default);
    Task<Result> ChangeUserStatusAsync(Guid userId, bool isActive, CancellationToken cancellationToken = default);
    Task<Result> AssignRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);
}

public class UserProfileResponse
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? PhoneNumber { get; set; }
    public string Role { get; set; } = string.Empty;
    public Guid? CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class UpdateProfileRequest
{
    public string? FullName { get; set; }
    public string? PhoneNumber { get; set; }
}

public class InviteUserRequest
{
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Password { get; set; }
    public Guid CompanyId { get; set; }
    public string Role { get; set; } = "Personnel";
}

public class InviteUserResponse
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? TemporaryPassword { get; set; }
    public bool EmailDeliveryPending { get; set; }
}

public class UserListItem
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
