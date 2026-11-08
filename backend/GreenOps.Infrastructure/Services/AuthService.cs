using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using GreenOps.Application.Abstractions.Repositories;
using GreenOps.Application.Abstractions.Services;
using GreenOps.Application.Common;
using GreenOps.Domain.Entities;
using GreenOps.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace GreenOps.Infrastructure.Services;

public class AuthService : IAuthService
{
    public const string TwoFactorRequiredError = "2FA_REQUIRED";

    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        ICompanyRepository companyRepository,
        IConfiguration configuration,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _companyRepository = companyRepository;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<Result<LoginResponse>> LoginAsync(string email, string password, string? twoFactorCode = null, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);
        if (user is null || !user.IsActive || (user.CompanyId.HasValue && (user.Company is null || !user.Company.IsActive)))
            return Result<LoginResponse>.Failure("Geçersiz e-posta veya şifre.");

        if (!PasswordHasher.Verify(password, user.PasswordHash, out var needsRehash))
            return Result<LoginResponse>.Failure("Geçersiz e-posta veya şifre.");

        if (user.TwoFactorEnabled)
        {
            if (string.IsNullOrWhiteSpace(twoFactorCode))
                return Result<LoginResponse>.Failure(TwoFactorRequiredError);
            if (string.IsNullOrEmpty(user.TwoFactorSecret) || !Totp.Verify(user.TwoFactorSecret, twoFactorCode))
                return Result<LoginResponse>.Failure("Geçersiz 2FA kodu.");
        }

        if (needsRehash)
        {
            // Eski SHA256 hash'i BCrypt'e taşı.
            user.PasswordHash = PasswordHasher.Hash(password);
            _userRepository.Update(user);
        }

        return Result<LoginResponse>.Success(await IssueTokensAsync(user, cancellationToken));
    }

    public async Task<Result<Guid>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var passwordError = PasswordPolicy.Validate(request.Password);
        if (passwordError is not null)
            return Result<Guid>.Failure(passwordError);

        var fullName = request.FullName?.Trim() ?? string.Empty;
        if (fullName.Length < 3 || fullName.Length > 200)
            return Result<Guid>.Failure("Ad Soyad 3-200 karakter olmalıdır.");

        var email = request.Email?.Trim() ?? string.Empty;
        if (email.Length is < 5 or > 200 || !email.Contains('@'))
            return Result<Guid>.Failure("Geçerli bir e-posta adresi giriniz.");

        var existingUser = await _userRepository.GetByEmailAsync(email, cancellationToken);
        if (existingUser is not null)
            return Result<Guid>.Failure("Bu e-posta adresi zaten kayıtlı.");

        // Kayıt her zaman yeni bir şirket (tenant) açar; kaydolan kişi onun yöneticisidir.
        var company = new Company
        {
            Id = Guid.NewGuid(),
            Name = $"{fullName} Şirketi",
            TaxNo = "",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
        await _companyRepository.AddAsync(company, cancellationToken);

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            FullName = fullName,
            Email = email,
            PasswordHash = PasswordHasher.Hash(request.Password),
            Role = UserRole.CompanyAdmin,
            CompanyId = company.Id,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
        await _userRepository.AddAsync(user, cancellationToken);

        // Şirket ve kullanıcı aynı DbContext'te; tek SaveChanges ile birlikte yazılır.
        await _userRepository.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(user.Id);
    }

    public async Task<Result<LoginResponse>> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return Result<LoginResponse>.Failure("Geçersiz veya süresi dolmuş refresh token.");

        var token = await _refreshTokenRepository.GetByTokenAsync(refreshToken, cancellationToken);
        if (token is null
            || token.ExpiresAt < DateTimeOffset.UtcNow
            || token.User is null
            || !token.User.IsActive
            || (token.User.CompanyId.HasValue && (token.User.Company is null || !token.User.Company.IsActive)))
            return Result<LoginResponse>.Failure("Geçersiz veya süresi dolmuş refresh token.");

        // Rotasyon: kullanılan token bir daha kullanılamaz.
        token.IsRevoked = true;
        _refreshTokenRepository.Update(token);

        return Result<LoginResponse>.Success(await IssueTokensAsync(token.User, cancellationToken));
    }

    public async Task<Result> ForgotPasswordAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);
        if (user is null || !user.IsActive || (user.CompanyId.HasValue && (user.Company is null || !user.Company.IsActive)))
            return Result.Success(); // Kullanıcı var/yok bilgisi verilmez.

        var resetToken = TokenHasher.NewUrlSafeToken();
        user.PasswordResetToken = TokenHasher.Sha256(resetToken);
        user.PasswordResetTokenExpiry = DateTimeOffset.UtcNow.AddHours(1);
        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync(cancellationToken);

        // E-posta servisi henüz yok. Geliştirmede token'ı test edebilmek için sadece
        // Auth:LogResetTokens=true iken log'a yazılır (appsettings.Development.json).
        if (bool.TryParse(_configuration["Auth:LogResetTokens"], out var logTokens) && logTokens)
            _logger.LogWarning("Şifre sıfırlama token'ı ({Email}): {Token}", user.Email, resetToken);

        return Result.Success();
    }

    public async Task<Result> ResetPasswordAsync(string token, string newPassword, CancellationToken cancellationToken = default)
    {
        var passwordError = PasswordPolicy.Validate(newPassword);
        if (passwordError is not null)
            return Result.Failure(passwordError);

        if (string.IsNullOrWhiteSpace(token))
            return Result.Failure("Geçersiz veya süresi dolmuş token.");

        var user = await _userRepository.GetByPasswordResetTokenAsync(TokenHasher.Sha256(token), cancellationToken);
        if (user is null || user.PasswordResetTokenExpiry is null || user.PasswordResetTokenExpiry < DateTimeOffset.UtcNow)
            return Result.Failure("Geçersiz veya süresi dolmuş token.");

        user.PasswordHash = PasswordHasher.Hash(newPassword);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;
        _userRepository.Update(user);

        // Şifre değişince açık oturumlar kapanır.
        await _refreshTokenRepository.RevokeAllForUserAsync(user.Id, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result<TwoFactorSetupResponse>> Enable2FAAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            return Result<TwoFactorSetupResponse>.Failure("Kullanıcı bulunamadı.");
        if (user.TwoFactorEnabled)
            return Result<TwoFactorSetupResponse>.Failure("2FA zaten aktif.");

        // Secret kaydedilir ama 2FA, verify-2fa ile geçerli bir kod doğrulanana kadar aktif olmaz.
        var secret = Totp.GenerateSecret();
        user.TwoFactorSecret = secret;
        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync(cancellationToken);

        var label = Uri.EscapeDataString($"GreenOps:{user.Email}");
        return Result<TwoFactorSetupResponse>.Success(new TwoFactorSetupResponse
        {
            Secret = secret,
            QrCodeUrl = $"otpauth://totp/{label}?secret={secret}&issuer=GreenOps&digits=6&period=30"
        });
    }

    public async Task<Result> Verify2FAAsync(Guid userId, string code, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            return Result.Failure("Kullanıcı bulunamadı.");

        if (string.IsNullOrEmpty(user.TwoFactorSecret))
            return Result.Failure("Önce 2FA kurulumu yapılmalıdır.");

        if (!Totp.Verify(user.TwoFactorSecret, code))
            return Result.Failure("Geçersiz 2FA kodu.");

        user.TwoFactorEnabled = true;
        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> LogoutAsync(Guid userId, string token, CancellationToken cancellationToken = default)
    {
        var refreshToken = await _refreshTokenRepository.GetByTokenAsync(token, cancellationToken);
        // Başkasının token'ı bu uçla iptal edilemez.
        if (refreshToken is not null && refreshToken.UserId == userId)
        {
            refreshToken.IsRevoked = true;
            _refreshTokenRepository.Update(refreshToken);
            await _refreshTokenRepository.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }

    private async Task<LoginResponse> IssueTokensAsync(AppUser user, CancellationToken cancellationToken)
    {
        var expiresMinutes = int.TryParse(_configuration["Jwt:ExpiresMinutes"], out var m) && m > 0 ? m : 60;

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
            CreatedAt = DateTimeOffset.UtcNow
        };
        await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);
        await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

        return new LoginResponse
        {
            AccessToken = GenerateAccessToken(user, expiresMinutes),
            RefreshToken = refreshToken.Token,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(expiresMinutes),
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role.ToString(),
            CompanyId = user.CompanyId
        };
    }

    private string GenerateAccessToken(AppUser user, int expiresMinutes)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, user.Role.ToString()),
        };

        if (user.CompanyId.HasValue)
            claims.Add(new Claim("companyId", user.CompanyId.Value.ToString()));

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiresMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
