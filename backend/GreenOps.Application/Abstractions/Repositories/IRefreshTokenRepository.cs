using GreenOps.Domain.Entities;

namespace GreenOps.Application.Abstractions.Repositories;

public interface IRefreshTokenRepository : IRepository<RefreshToken>
{
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>Kullanıcının tüm aktif refresh token'larını iptal eder (kaydetmez).</summary>
    Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
