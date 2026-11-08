namespace GreenOps.Domain.Entities;

public class ExportCarbonPassport
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductCode { get; set; }
    public string? ShipmentReference { get; set; }
    public decimal Quantity { get; set; }
    public string QuantityUnit { get; set; } = "ton";
    public DateTimeOffset ProductionStart { get; set; }
    public DateTimeOffset ProductionEnd { get; set; }
    public decimal TotalEmissionsKg { get; set; }
    public decimal EmissionsPerUnitKg { get; set; }
    public decimal DataReliabilityPercent { get; set; }
    public string PublicTokenHash { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? RevokedAt { get; set; }

    public Company? Company { get; set; }
    public ICollection<ExportCarbonPassportEntry> Entries { get; set; } = new List<ExportCarbonPassportEntry>();
}

public class ExportCarbonPassportEntry
{
    public Guid PassportId { get; set; }
    public Guid EmissionEntryId { get; set; }

    public ExportCarbonPassport? Passport { get; set; }
    public EmissionEntry? EmissionEntry { get; set; }
}
