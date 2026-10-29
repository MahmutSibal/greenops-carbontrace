using GreenOps.Application.Common;

namespace GreenOps.Application.Abstractions.Services;

public interface IAuthService
{
    Task<Result<LoginResponse>> LoginAsync(string email, string password, string? twoFactorCode = null, CancellationToken cancellationToken = default);
    Task<Result<Guid>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<Result<LoginResponse>> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<Result> ForgotPasswordAsync(string email, CancellationToken cancellationToken = default);
    Task<Result> ResetPasswordAsync(string token, string newPassword, CancellationToken cancellationToken = default);
    Task<Result<TwoFactorSetupResponse>> Enable2FAAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Result> Verify2FAAsync(Guid userId, string code, CancellationToken cancellationToken = default);
    Task<Result> LogoutAsync(Guid userId, string token, CancellationToken cancellationToken = default);
}

public class TwoFactorSetupResponse
{
    public string Secret { get; set; } = string.Empty;
    public string QrCodeUrl { get; set; } = string.Empty;
}

public class LoginResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public Guid? CompanyId { get; set; }
}

public class RegisterRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    // Rol ve şirket istemciden alınmaz: kayıt her zaman yeni bir şirket açar ve kullanıcı onun
    // CompanyAdmin'i olur. Mevcut bir şirkete katılım sadece davet (users/invite) ile yapılır.
}
