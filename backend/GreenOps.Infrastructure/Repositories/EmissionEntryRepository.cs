using GreenOps.Application.Abstractions.Repositories;
using GreenOps.Domain.Entities;
using GreenOps.Infrastructure.Persistence;

namespace GreenOps.Infrastructure.Repositories;

public class EmissionEntryRepository : RepositoryBase<EmissionEntry>, IEmissionEntryRepository
{
    public EmissionEntryRepository(AppDbContext dbContext) : base(dbContext)
    {
    }
}
