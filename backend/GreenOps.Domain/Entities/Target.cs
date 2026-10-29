namespace GreenOps.Domain.Entities;

public class Target
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public int TargetYear { get; set; }
    public decimal ReductionPercent { get; set; }
    public int BaselineYear { get; set; }
    public decimal BaselineEmissions { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Company? Company { get; set; }
}
