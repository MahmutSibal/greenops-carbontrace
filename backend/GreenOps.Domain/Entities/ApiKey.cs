namespace GreenOps.Domain.Entities;

public class ApiKey
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;           // ERP-Entegrasyon, IoT-Gateway vb.
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastUsedAt { get; set; }

    public Company? Company { get; set; }
}
