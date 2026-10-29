using GreenOps.Application.Abstractions.Repositories;
using GreenOps.Domain.Entities;
using GreenOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GreenOps.Infrastructure.Repositories;

public class SupplierEmissionRepository : RepositoryBase<SupplierEmission>, ISupplierEmissionRepository
{
    public SupplierEmissionRepository(AppDbContext dbContext) : base(dbContext) { }

    public async Task<IReadOnlyList<SupplierEmission>> ListBySupplierAsync(Guid supplierId, CancellationToken ct = default)
        => await DbContext.SupplierEmissions.Where(e => e.SupplierId == supplierId).OrderByDescending(e => e.Year).ToListAsync(ct);
}
