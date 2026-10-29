namespace GreenOps.Domain.Entities;

public class BillingPlan
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;           // Basic, Pro, Enterprise
    public decimal MonthlyPrice { get; set; }
    public int MaxFacilities { get; set; }
    public int MaxUsersCount { get; set; }
    public bool HasApiAccess { get; set; }
    public bool HasIotIntegration { get; set; }
    public bool HasAiPrediction { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
