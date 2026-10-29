using GreenOps.Domain.Entities;

namespace GreenOps.Application.Abstractions.Repositories;

public interface IDocumentRepository : IRepository<Document>
{
    Task<IReadOnlyList<Document>> ListByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);
}
