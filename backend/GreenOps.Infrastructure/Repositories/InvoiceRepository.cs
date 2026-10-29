using GreenOps.Application.Abstractions.Repositories;
using GreenOps.Domain.Entities;
using GreenOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GreenOps.Infrastructure.Repositories;

public class InvoiceRepository : RepositoryBase<Invoice>, IInvoiceRepository
{
    public InvoiceRepository(AppDbContext dbContext) : base(dbContext) { }

    public async Task<IReadOnlyList<Invoice>> ListByCompanyAsync(Guid companyId, CancellationToken ct = default)
        => await DbContext.Invoices.Where(i => i.CompanyId == companyId).OrderByDescending(i => i.InvoiceDate).ToListAsync(ct);
}
