using GreenOps.Domain.Entities;

namespace GreenOps.Application.Abstractions.Repositories;

public interface ICarbonCreditRepository : IRepository<CarbonCredit>
{
    Task<IReadOnlyList<CarbonCredit>> ListByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);
}
