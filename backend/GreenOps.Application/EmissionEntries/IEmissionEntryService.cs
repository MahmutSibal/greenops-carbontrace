using GreenOps.Application.Common;
using GreenOps.Domain.Entities;

namespace GreenOps.Application.EmissionEntries;

public interface IEmissionEntryService
{
    /// <param name="companyScope">Null değilse tesis bu şirkete ait olmalıdır (SuperAdmin için null).</param>
    Task<Result<Guid>> CreateAsync(EmissionEntry entry, Guid? companyScope, CancellationToken cancellationToken = default);
    Task<Result<EmissionEntry>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<PagedResult<EmissionEntry>>> GetPagedAsync(EmissionQuery query, CancellationToken cancellationToken = default);
    Task<Result> ApproveAsync(Guid id, Guid approvedByUserId, bool allowSelfApproval, CancellationToken cancellationToken = default);
    Task<Result> RejectAsync(Guid id, string reason, Guid rejectedByUserId, CancellationToken cancellationToken = default);
    Task<Result> UpdateAsync(Guid id, decimal? value, Guid? categoryId, CancellationToken cancellationToken = default);
    Task<Result> UpdateEvidenceUrlAsync(Guid id, string evidenceUrl, CancellationToken cancellationToken = default);
    Task<Result> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public class EmissionQuery
{
    public Guid? CompanyId { get; set; }
    public Guid? FacilityId { get; set; }
    public Guid? CategoryId { get; set; }
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
