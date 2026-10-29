using GreenOps.Application.Common;

namespace GreenOps.Application.Abstractions.Services;

public interface ISupplierService
{
    Task<Result<IReadOnlyList<SupplierDto>>> ListByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<Result> InviteSupplierAsync(InviteSupplierRequest request, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<SupplierEmissionDto>>> GetSupplierEmissionsAsync(Guid supplierId, CancellationToken cancellationToken = default);
    Task<Result> UpdateESGScoreAsync(Guid supplierId, int score, CancellationToken cancellationToken = default);
}

public class SupplierDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public int ESGScore { get; set; }
    public bool IsActive { get; set; }
}

public class InviteSupplierRequest
{
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
}

public class SupplierEmissionDto
{
    public Guid Id { get; set; }
    public int Year { get; set; }
    public decimal Value { get; set; }
    public string? Description { get; set; }
}
