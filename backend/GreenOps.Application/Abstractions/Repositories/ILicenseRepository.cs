using GreenOps.Domain.Entities;

namespace GreenOps.Application.Abstractions.Repositories;

public interface ILicenseRepository : IRepository<License>
{
    Task<License?> GetByKeyAsync(string licenseKey, CancellationToken cancellationToken = default);
    Task<License?> GetActiveByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<List<License>> GetAllByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);
}
