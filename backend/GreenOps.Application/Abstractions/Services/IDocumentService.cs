using GreenOps.Application.Common;

namespace GreenOps.Application.Abstractions.Services;

public interface IDocumentService
{
    Task<Result<DocumentUploadResponse>> UploadAsync(Guid companyId, string fileName, string contentType, long fileSize, Stream fileStream, Guid? emissionEntryId = null, CancellationToken cancellationToken = default);
    Task<Result<DocumentDownloadResponse>> DownloadAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<DocumentListItem>>> ListByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);
}

public class DocumentUploadResponse
{
    public Guid Id { get; set; }
    public string FileUrl { get; set; } = string.Empty;
}

public class DocumentDownloadResponse
{
    public Guid CompanyId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string PhysicalPath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
}

public class DocumentListItem
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}
