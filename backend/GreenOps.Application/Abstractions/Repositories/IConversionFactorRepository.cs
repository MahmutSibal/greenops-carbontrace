using GreenOps.Domain.Entities;

namespace GreenOps.Application.Abstractions.Repositories;

public interface IConversionFactorRepository : IRepository<ConversionFactor>
{
    Task<ConversionFactor?> GetFactorAsync(Guid categoryId, int year, CancellationToken cancellationToken = default);

    /// <summary>İstenen yılın katsayısı; yoksa o yıldan önceki en güncel katsayı.</summary>
    Task<ConversionFactor?> GetApplicableFactorAsync(Guid categoryId, int year, CancellationToken cancellationToken = default);
}
