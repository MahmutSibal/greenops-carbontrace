using GreenOps.Application.Abstractions.Repositories;
using GreenOps.Domain.Entities;
using GreenOps.Infrastructure.Persistence;

namespace GreenOps.Infrastructure.Repositories;

public class ScopeRepository : RepositoryBase<Scope>, IScopeRepository
{
    public ScopeRepository(AppDbContext dbContext) : base(dbContext) { }
}
