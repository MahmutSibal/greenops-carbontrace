namespace GreenOps.Application.Abstractions.Services;

/// <summary>İsteği yapan kullanıcı. HTTP isteği dışında (seed, migration) tüm alanlar null olur.</summary>
public interface ICurrentUserAccessor
{
    Guid? UserId { get; }
    string? UserName { get; }
    Guid? CompanyId { get; }
}
