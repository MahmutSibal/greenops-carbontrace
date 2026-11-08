using System.Security.Cryptography;
using GreenOps.Application.Abstractions.Repositories;
using GreenOps.Application.Abstractions.Services;
using GreenOps.Application.Common;
using GreenOps.Domain.Entities;
using GreenOps.Infrastructure.Persistence;
using GreenOps.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace GreenOps.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IFileStorage _fileStorage;
    private readonly AppDbContext _dbContext;

    public UserService(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IFileStorage fileStorage,
        AppDbContext dbContext)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _fileStorage = fileStorage;
        _dbContext = dbContext;
    }

    public async Task<Result<UserProfileResponse>> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .Include(u => u.Company)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
            return Result<UserProfileResponse>.Failure("Kullanıcı bulunamadı.");

        return Result<UserProfileResponse>.Success(new UserProfileResponse
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            AvatarUrl = user.AvatarUrl is null ? null : "/api/users/me/avatar",
            PhoneNumber = user.PhoneNumber,
            Role = user.Role.ToString(),
            CompanyId = user.CompanyId,
            CompanyName = user.Company?.Name,
            TwoFactorEnabled = user.TwoFactorEnabled,
            CreatedAt = user.CreatedAt
        });
    }

    public async Task<Result> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            return Result.Failure("Kullanıcı bulunamadı.");

        if (!string.IsNullOrWhiteSpace(request.FullName))
        {
            var fullName = request.FullName.Trim();
            if (fullName.Length is < 3 or > 200)
                return Result.Failure("Ad Soyad 3-200 karakter olmalıdır.");
            user.FullName = fullName;
        }
        if (request.PhoneNumber is not null)
        {
            if (request.PhoneNumber.Length > 30)
                return Result.Failure("Telefon numarası en fazla 30 karakter olabilir.");
            user.PhoneNumber = request.PhoneNumber;
        }

        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            return Result.Failure("Kullanıcı bulunamadı.");

        if (!PasswordHasher.Verify(currentPassword, user.PasswordHash, out _))
            return Result.Failure("Mevcut şifre yanlış.");

        var passwordError = PasswordPolicy.Validate(newPassword);
        if (passwordError is not null)
            return Result.Failure(passwordError);

        user.PasswordHash = PasswordHasher.Hash(newPassword);
        _userRepository.Update(user);

        // Diğer cihazlardaki oturumlar kapanır.
        await _refreshTokenRepository.RevokeAllForUserAsync(userId, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result<string>> UploadAvatarAsync(Guid userId, string fileName, long length, Stream fileStream, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            return Result<string>.Failure("Kullanıcı bulunamadı.");

        var saved = await _fileStorage.SaveAsync(FileCategory.Avatar, fileName, length, fileStream, cancellationToken);
        if (!saved.IsSuccess)
            return Result<string>.Failure(saved.Error!);

        var previous = user.AvatarUrl;
        user.AvatarUrl = saved.Value;
        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrEmpty(previous))
            _fileStorage.Delete(previous);

        return Result<string>.Success("/api/users/me/avatar");
    }

    public async Task<Result<IReadOnlyList<UserListItem>>> ListByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var users = await _dbContext.Users
            .Where(u => u.CompanyId == companyId)
            .OrderBy(u => u.FullName)
            .Select(u => new UserListItem
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                Role = u.Role.ToString(),
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<UserListItem>>.Success(users);
    }

    /// <remarks>Şirket ve rol yetkisi çağıran kontrolcüde doğrulanır.</remarks>
    public async Task<Result<InviteUserResponse>> InviteUserAsync(InviteUserRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email?.Trim() ?? string.Empty;
        if (email.Length is < 5 or > 200 || !email.Contains('@'))
            return Result<InviteUserResponse>.Failure("Geçerli bir e-posta adresi giriniz.");

        var fullName = request.FullName?.Trim() ?? string.Empty;
        if (fullName.Length is < 3 or > 200)
            return Result<InviteUserResponse>.Failure("Ad Soyad 3-200 karakter olmalıdır.");

        if (!Enum.TryParse<UserRole>(request.Role, true, out var role))
            return Result<InviteUserResponse>.Failure("Geçersiz rol.");

        if (!await _dbContext.Companies.AnyAsync(c => c.Id == request.CompanyId, cancellationToken))
            return Result<InviteUserResponse>.Failure("Şirket bulunamadı.");

        var existing = await _userRepository.GetByEmailAsync(email, cancellationToken);
        if (existing is not null)
            return Result<InviteUserResponse>.Failure("Bu e-posta adresi zaten kayıtlı.");

        var password = request.Password;
        string? temporaryPassword = null;
        if (!string.IsNullOrWhiteSpace(password))
        {
            var passwordError = PasswordPolicy.Validate(password);
            if (passwordError is not null)
                return Result<InviteUserResponse>.Failure(passwordError);
        }
        else
        {
            temporaryPassword = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16)) + "A1";
            password = temporaryPassword;
        }

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            FullName = fullName,
            Email = email,
            PasswordHash = PasswordHasher.Hash(password),
            Role = role,
            CompanyId = request.CompanyId,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _userRepository.AddAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        return Result<InviteUserResponse>.Success(new InviteUserResponse
        {
            UserId = user.Id,
            Email = user.Email,
            TemporaryPassword = temporaryPassword,
            EmailDeliveryPending = true
        });
    }

    public async Task<Result> ChangeUserStatusAsync(Guid userId, bool isActive, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            return Result.Failure("Kullanıcı bulunamadı.");

        user.IsActive = isActive;
        _userRepository.Update(user);
        if (!isActive)
            await _refreshTokenRepository.RevokeAllForUserAsync(userId, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> AssignRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            return Result.Failure("Kullanıcı bulunamadı.");

        var customRole = await _roleRepository.GetByIdAsync(roleId, cancellationToken);
        if (customRole is null)
            return Result.Failure("Rol bulunamadı.");

        user.RoleId = roleId;
        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
