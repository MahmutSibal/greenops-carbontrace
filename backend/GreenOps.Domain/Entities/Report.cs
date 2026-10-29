namespace GreenOps.Domain.Entities;

public class Report
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string ReportType { get; set; } = string.Empty;     // ISO-14064, GHG-Protocol, Custom
    public int Year { get; set; }
    public string? FileUrl { get; set; }
    public string Status { get; set; } = "Generated";          // Generated, Downloaded, Archived
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Company? Company { get; set; }
}
