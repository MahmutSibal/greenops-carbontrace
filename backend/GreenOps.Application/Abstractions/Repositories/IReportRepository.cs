using GreenOps.Domain.Entities;

namespace GreenOps.Application.Abstractions.Repositories;

public interface IReportRepository : IRepository<Report>
{
    Task<IReadOnlyList<Report>> ListByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);
}
