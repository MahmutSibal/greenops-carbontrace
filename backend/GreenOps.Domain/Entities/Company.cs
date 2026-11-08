namespace GreenOps.Domain.Entities;

public class Company
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string TaxNo { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Industry { get; set; }
    public int? EmployeeCount { get; set; }
    public decimal? AnnualRevenue { get; set; }
    public Guid? BillingPlanId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public BillingPlan? BillingPlan { get; set; }
    public ICollection<Facility> Facilities { get; set; } = new List<Facility>();
    public ICollection<License> Licenses { get; set; } = new List<License>();
    public ICollection<AppUser> Users { get; set; } = new List<AppUser>();
    public ICollection<Supplier> Suppliers { get; set; } = new List<Supplier>();
    public ICollection<Target> Targets { get; set; } = new List<Target>();
    public ICollection<CarbonCredit> CarbonCredits { get; set; } = new List<CarbonCredit>();
    public ICollection<Document> Documents { get; set; } = new List<Document>();
    public ICollection<AlertSetting> AlertSettings { get; set; } = new List<AlertSetting>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    public ICollection<ApiKey> ApiKeys { get; set; } = new List<ApiKey>();
    public ICollection<Report> Reports { get; set; } = new List<Report>();
    public ICollection<ExportCarbonPassport> ExportCarbonPassports { get; set; } = new List<ExportCarbonPassport>();
}
