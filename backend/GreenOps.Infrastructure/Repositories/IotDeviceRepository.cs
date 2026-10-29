using GreenOps.Application.Abstractions.Repositories;
using GreenOps.Domain.Entities;
using GreenOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GreenOps.Infrastructure.Repositories;

public class IotDeviceRepository : RepositoryBase<IotDevice>, IIotDeviceRepository
{
    public IotDeviceRepository(AppDbContext dbContext) : base(dbContext) { }

    public async Task<IReadOnlyList<IotDevice>> ListByFacilityAsync(Guid facilityId, CancellationToken ct = default)
        => await DbContext.IotDevices.Where(d => d.FacilityId == facilityId && d.IsActive).ToListAsync(ct);
}
