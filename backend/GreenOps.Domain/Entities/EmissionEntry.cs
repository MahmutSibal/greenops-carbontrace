namespace GreenOps.Domain.Entities;

public enum EmissionEntryStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}

public class EmissionEntry
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }                     // Tesisin kayıt anındaki şirketi (tenant filtresi için)
    public Guid FacilityId { get; set; }
    public Guid CategoryId { get; set; }
    public decimal Value { get; set; }
    public string? Unit { get; set; }                       // Kayıt anındaki kategori birimi
    public decimal CalculationResult { get; set; }
    public Guid? ConversionFactorId { get; set; }           // Hesaplamada kullanılan katsayı
    public decimal? FactorValue { get; set; }               // Katsayının kayıt anındaki değeri (denetim izi)
    public DateTimeOffset? PeriodStart { get; set; }        // Faaliyet dönemi başlangıcı
    public DateTimeOffset? PeriodEnd { get; set; }          // Faaliyet dönemi bitişi
    public string? EvidenceUrl { get; set; }
    public string? RejectionReason { get; set; }
    public EmissionEntryStatus Status { get; set; } = EmissionEntryStatus.Pending;
    public Guid? ApprovedByUserId { get; set; }
    public Guid? RejectedByUserId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public DateTimeOffset? RejectedAt { get; set; }
    public bool IsDeleted { get; set; } = false;

    public Company? Company { get; set; }
    public Facility? Facility { get; set; }
    public EmissionCategory? Category { get; set; }
    public ConversionFactor? ConversionFactor { get; set; }
    public AppUser? ApprovedByUser { get; set; }
    public AppUser? RejectedByUser { get; set; }
    public AppUser? CreatedByUser { get; set; }
    public ICollection<Document> Documents { get; set; } = new List<Document>();
}
