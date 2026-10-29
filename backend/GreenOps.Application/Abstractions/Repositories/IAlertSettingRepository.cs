using GreenOps.Domain.Entities;

namespace GreenOps.Application.Abstractions.Repositories;

public interface IAlertSettingRepository : IRepository<AlertSetting>
{
    Task<IReadOnlyList<AlertSetting>> ListByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);
}
