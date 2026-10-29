namespace GreenOps.Domain.Entities;

public class Supplier
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string? ContactPhone { get; set; }
    public string? Address { get; set; }
    public int ESGScore { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Company? Company { get; set; }
    public ICollection<SupplierEmission> Emissions { get; set; } = new List<SupplierEmission>();
}
