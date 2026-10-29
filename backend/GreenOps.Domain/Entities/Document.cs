namespace GreenOps.Domain.Entities;

public class Document
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid? EmissionEntryId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Company? Company { get; set; }
    public EmissionEntry? EmissionEntry { get; set; }
}
