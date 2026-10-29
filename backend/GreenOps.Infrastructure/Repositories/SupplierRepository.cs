using GreenOps.Application.Abstractions.Repositories;
using GreenOps.Domain.Entities;
using GreenOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GreenOps.Infrastructure.Repositories;

public class SupplierRepository : RepositoryBase<Supplier>, ISupplierRepository
{
    public SupplierRepository(AppDbContext dbContext) : base(dbContext) { }

    public async Task<IReadOnlyList<Supplier>> ListByCompanyAsync(Guid companyId, CancellationToken ct = default)
        => await DbContext.Suppliers.Where(s => s.CompanyId == companyId && s.IsActive).ToListAsync(ct);
}
