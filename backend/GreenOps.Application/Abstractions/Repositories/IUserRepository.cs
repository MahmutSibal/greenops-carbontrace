using GreenOps.Domain.Entities;

namespace GreenOps.Application.Abstractions.Repositories;

public interface IUserRepository : IRepository<AppUser>
{
    Task<AppUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<AppUser?> GetByPasswordResetTokenAsync(string tokenHash, CancellationToken cancellationToken = default);
}
