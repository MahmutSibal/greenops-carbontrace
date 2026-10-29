namespace GreenOps.Domain.Entities;

public class AlertSetting
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string AlertType { get; set; } = string.Empty;      // QuotaExceeded, MonthlyLimit, etc.
    public decimal Threshold { get; set; }                       // Yüzde veya mutlak değer
    public bool NotifyEmail { get; set; } = true;
    public bool NotifySms { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Company? Company { get; set; }
}
