using GreenOps.Domain.Entities;

namespace GreenOps.Application.Abstractions.Repositories;

public interface IIotDeviceRepository : IRepository<IotDevice>
{
    Task<IReadOnlyList<IotDevice>> ListByFacilityAsync(Guid facilityId, CancellationToken cancellationToken = default);
}
