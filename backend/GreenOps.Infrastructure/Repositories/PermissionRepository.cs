using GreenOps.Application.Abstractions.Repositories;
using GreenOps.Domain.Entities;
using GreenOps.Infrastructure.Persistence;

namespace GreenOps.Infrastructure.Repositories;

public class PermissionRepository : RepositoryBase<Permission>, IPermissionRepository
{
    public PermissionRepository(AppDbContext dbContext) : base(dbContext) { }
}
