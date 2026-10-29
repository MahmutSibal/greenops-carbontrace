using GreenOps.Application.Abstractions.Services;
using GreenOps.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GreenOps.WebApi.Controllers;

[Route("api/auth")]
[EnableRateLimiting(RateLimitPolicies.Auth)]
public class AuthController : ApiControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>POST /api/auth/login — E-posta/şifre (ve 2FA aktifse kod) ile giriş (JWT döner)</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await _authService.LoginAsync(request.Email, request.Password, request.TwoFactorCode, ct);
        if (result.IsSuccess) return Ok(result.Value);

        if (result.Error == AuthService.TwoFactorRequiredError)
            return Unauthorized(new { error = "İki aşamalı doğrulama kodu gerekli.", twoFactorRequired = true });
        return Unauthorized(new { error = result.Error });
    }

    /// <summary>POST /api/auth/register — Yeni şirket (tenant) ve yöneticisini oluşturur</summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var result = await _authService.RegisterAsync(request, ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });

        // Register sonrası istemciyi doğrudan oturumlu hale getir.
        var loginResult = await _authService.LoginAsync(request.Email, request.Password, null, ct);
        if (!loginResult.IsSuccess) return BadRequest(new { error = loginResult.Error });
        return Ok(loginResult.Value);
    }

    /// <summary>POST /api/auth/refresh-token — Süresi dolan token'ı yeniler</summary>
    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken ct)
    {
        var result = await _authService.RefreshTokenAsync(request.RefreshToken, ct);
        if (!result.IsSuccess) return Unauthorized(new { error = result.Error });
        return Ok(result.Value);
    }

    /// <summary>POST /api/auth/forgot-password — Şifre sıfırlama token'ı üretir</summary>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken ct)
    {
        await _authService.ForgotPasswordAsync(request.Email, ct);
        // Kullanıcının varlığı sızdırılmasın diye her durumda aynı yanıt.
        return Ok(new { message = "Hesap kayıtlıysa şifre sıfırlama bağlantısı gönderilecek." });
    }

    /// <summary>POST /api/auth/reset-password — Gelen token ile şifreyi yeniler</summary>
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken ct)
    {
        var result = await _authService.ResetPasswordAsync(request.Token, request.NewPassword, ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(new { message = "Şifre başarıyla sıfırlandı." });
    }

    /// <summary>POST /api/auth/enable-2fa — TOTP secret üretir (verify-2fa ile aktifleşir)</summary>
    [HttpPost("enable-2fa")]
    [Authorize]
    public async Task<IActionResult> Enable2FA(CancellationToken ct)
    {
        if (CurrentUserId is not { } userId) return Unauthorized();
        var result = await _authService.Enable2FAAsync(userId, ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

    /// <summary>POST /api/auth/verify-2fa — Authenticator kodunu doğrular ve 2FA'yı aktif eder</summary>
    [HttpPost("verify-2fa")]
    [Authorize]
    public async Task<IActionResult> Verify2FA([FromBody] Verify2FARequest request, CancellationToken ct)
    {
        if (CurrentUserId is not { } userId) return Unauthorized();
        var result = await _authService.Verify2FAAsync(userId, request.Code, ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(new { message = "2FA doğrulaması başarılı." });
    }

    /// <summary>POST /api/auth/logout — Oturumu sonlandırır, refresh token iptal edilir</summary>
    [HttpPost("logout")]
    [Authorize]
    [DisableRateLimiting]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest? request, CancellationToken ct)
    {
        if (CurrentUserId is not { } userId) return Unauthorized();
        var result = await _authService.LogoutAsync(userId, request?.RefreshToken ?? string.Empty, ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(new { message = "Oturum sonlandırıldı." });
    }
}

public static class RateLimitPolicies
{
    public const string Auth = "auth";
    public const string Webhook = "webhook";
    public const string Ai = "ai";
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? TwoFactorCode { get; set; }
}
public class RefreshTokenRequest { public string RefreshToken { get; set; } = string.Empty; }
public class ForgotPasswordRequest { public string Email { get; set; } = string.Empty; }
public class ResetPasswordRequest { public string Token { get; set; } = string.Empty; public string NewPassword { get; set; } = string.Empty; }
public class Verify2FARequest { public string Code { get; set; } = string.Empty; }
public class LogoutRequest { public string RefreshToken { get; set; } = string.Empty; }
