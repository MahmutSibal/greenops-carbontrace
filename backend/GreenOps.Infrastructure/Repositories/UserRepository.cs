using GreenOps.Application.Abstractions.Repositories;
using GreenOps.Domain.Entities;
using GreenOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GreenOps.Infrastructure.Repositories;

public class UserRepository : RepositoryBase<AppUser>, IUserRepository
{
    public UserRepository(AppDbContext dbContext) : base(dbContext) { }

    public async Task<AppUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim();
        return await DbContext.Users
            .FirstOrDefaultAsync(u => u.Email == normalized, cancellationToken);
    }

    public async Task<AppUser?> GetByPasswordResetTokenAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return await DbContext.Users
            .FirstOrDefaultAsync(u => u.PasswordResetToken == tokenHash, cancellationToken);
    }
}
