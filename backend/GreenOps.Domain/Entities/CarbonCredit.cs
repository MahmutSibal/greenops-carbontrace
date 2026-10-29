namespace GreenOps.Domain.Entities;

public class CarbonCredit
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public decimal Credits { get; set; }
    public string? CertificateUrl { get; set; }
    public string? Provider { get; set; }
    public DateTimeOffset PurchaseDate { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Company? Company { get; set; }
}
