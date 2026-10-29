using GreenOps.Domain.Entities;

namespace GreenOps.Application.Abstractions.Repositories;

public interface IApiKeyRepository : IRepository<ApiKey>
{
    Task<IReadOnlyList<ApiKey>> ListByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <param name="keyHash">Anahtarın SHA-256 hash'i (DB'de düz anahtar saklanmaz).</param>
    Task<ApiKey?> GetByKeyAsync(string keyHash, CancellationToken cancellationToken = default);
}
