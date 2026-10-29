namespace GreenOps.Domain.Entities;

public class Facility
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public decimal? SquareMeters { get; set; }
    public int? EmployeeCount { get; set; }
    public string? Department { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Company? Company { get; set; }
    public ICollection<EmissionEntry> EmissionEntries { get; set; } = new List<EmissionEntry>();
    public ICollection<IotDevice> IotDevices { get; set; } = new List<IotDevice>();
}
