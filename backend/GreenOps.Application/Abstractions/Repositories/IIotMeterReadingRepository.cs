using GreenOps.Domain.Entities;

namespace GreenOps.Application.Abstractions.Repositories;

public interface IIotMeterReadingRepository : IRepository<IotMeterReading>
{
    Task<IReadOnlyList<IotMeterReading>> ListByDeviceAsync(Guid deviceId, CancellationToken cancellationToken = default);
}
