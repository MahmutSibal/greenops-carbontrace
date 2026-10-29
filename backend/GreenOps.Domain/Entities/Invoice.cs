namespace GreenOps.Domain.Entities;

public class Invoice
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "TRY";
    public string Status { get; set; } = "Pending";           // Pending, Paid, Overdue
    public string? PdfUrl { get; set; }
    public DateTimeOffset InvoiceDate { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Company? Company { get; set; }
}
