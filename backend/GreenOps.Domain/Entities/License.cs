namespace GreenOps.Domain.Entities;

public class License
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string LicenseKey { get; set; } = string.Empty;
    public string PlanName { get; set; } = "Free";
    public int MaxFacilities { get; set; } = 1;
    public int MaxUsers { get; set; } = 3;
    public string? MacAddress { get; set; }
    public string? IpAddress { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ActivatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }

    public Company? Company { get; set; }
}
