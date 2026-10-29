using GreenOps.Application.Abstractions.Repositories;
using GreenOps.Domain.Entities;
using GreenOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GreenOps.Infrastructure.Repositories;

public class IotMeterReadingRepository : RepositoryBase<IotMeterReading>, IIotMeterReadingRepository
{
    public IotMeterReadingRepository(AppDbContext dbContext) : base(dbContext) { }

    public async Task<IReadOnlyList<IotMeterReading>> ListByDeviceAsync(Guid deviceId, CancellationToken ct = default)
        => await DbContext.IotMeterReadings.Where(r => r.IotDeviceId == deviceId).OrderByDescending(r => r.ReadingTime).ToListAsync(ct);
}
