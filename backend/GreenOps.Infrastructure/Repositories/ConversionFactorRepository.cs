using GreenOps.Application.Abstractions.Repositories;
using GreenOps.Domain.Entities;
using GreenOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GreenOps.Infrastructure.Repositories;

public class ConversionFactorRepository : RepositoryBase<ConversionFactor>, IConversionFactorRepository
{
    public ConversionFactorRepository(AppDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<ConversionFactor?> GetFactorAsync(Guid categoryId, int year, CancellationToken cancellationToken = default)
    {
        // (CategoryId, Year) unique olduğu için en fazla bir kayıt döner.
        return await DbContext.ConversionFactors
            .FirstOrDefaultAsync(cf => cf.CategoryId == categoryId && cf.Year == year, cancellationToken);
    }

    public async Task<ConversionFactor?> GetApplicableFactorAsync(Guid categoryId, int year, CancellationToken cancellationToken = default)
    {
        // Yeni yılın resmi katsayısı yayımlanana kadar bir önceki yılın katsayısı kullanılır;
        // hangi katsayının kullanıldığı kayıtta ConversionFactorId olarak saklanır.
        return await DbContext.ConversionFactors
            .Where(cf => cf.CategoryId == categoryId && cf.IsActive && cf.Year <= year)
            .OrderByDescending(cf => cf.Year)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
