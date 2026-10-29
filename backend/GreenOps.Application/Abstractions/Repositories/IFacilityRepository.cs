using GreenOps.Domain.Entities;

namespace GreenOps.Application.Abstractions.Repositories;

public interface IFacilityRepository : IRepository<Facility>
{
    Task<IReadOnlyList<Facility>> ListByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);
}
