using GreenOps.Application.Common;

namespace GreenOps.Application.Abstractions.Services;

public interface ILicenseService
{
    Task<Result<LicenseCreateResponse>> CreateAsync(CreateLicenseRequest request, CancellationToken cancellationToken = default);
    Task<Result> ActivateAsync(ActivateLicenseRequest request, CancellationToken cancellationToken = default);
    Task<Result<LicenseStatusResponse>> GetStatusAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<Result<List<LicenseListItem>>> GetAllByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);
}

public class CreateLicenseRequest
{
    public Guid CompanyId { get; set; }
    public string PlanName { get; set; } = "Free";
    public int MaxFacilities { get; set; } = 1;
    public int MaxUsers { get; set; } = 3;
    public int DurationDays { get; set; } = 365;
}

public class LicenseCreateResponse
{
    public Guid Id { get; set; }
    public string LicenseKey { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
}

public class ActivateLicenseRequest
{
    public string LicenseKey { get; set; } = string.Empty;
    public string MacAddress { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
}

public class LicenseStatusResponse
{
    public bool IsActive { get; set; }
    public string LicenseKey { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public int MaxFacilities { get; set; }
    public int MaxUsers { get; set; }
    public string? MacAddress { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}

public class LicenseListItem
{
    public Guid Id { get; set; }
    public string LicenseKey { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int MaxFacilities { get; set; }
    public int MaxUsers { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}
