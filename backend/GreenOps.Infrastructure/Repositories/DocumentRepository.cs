using GreenOps.Application.Abstractions.Repositories;
using GreenOps.Domain.Entities;
using GreenOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GreenOps.Infrastructure.Repositories;

public class DocumentRepository : RepositoryBase<Document>, IDocumentRepository
{
    public DocumentRepository(AppDbContext dbContext) : base(dbContext) { }

    public async Task<IReadOnlyList<Document>> ListByCompanyAsync(Guid companyId, CancellationToken ct = default)
        => await DbContext.Documents.Where(d => d.CompanyId == companyId).OrderByDescending(d => d.CreatedAt).ToListAsync(ct);
}
