using GreenOps.Application.Abstractions.Repositories;
using GreenOps.Application.Abstractions.Services;
using GreenOps.Application.Common;
using GreenOps.Domain.Entities;
using GreenOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GreenOps.Infrastructure.Services;

public class DocumentService : IDocumentService
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IFileStorage _fileStorage;
    private readonly AppDbContext _dbContext;

    public DocumentService(IDocumentRepository documentRepository, IFileStorage fileStorage, AppDbContext dbContext)
    {
        _documentRepository = documentRepository;
        _fileStorage = fileStorage;
        _dbContext = dbContext;
    }

    public async Task<Result<DocumentUploadResponse>> UploadAsync(
        Guid companyId, string fileName, string contentType, long fileSize,
        Stream fileStream, Guid? emissionEntryId = null, CancellationToken cancellationToken = default)
    {
        if (emissionEntryId.HasValue
            && !await _dbContext.EmissionEntries.AnyAsync(e => e.Id == emissionEntryId.Value && e.CompanyId == companyId && !e.IsDeleted, cancellationToken))
            return Result<DocumentUploadResponse>.Failure("Emisyon kaydı bulunamadı.");

        var saved = await _fileStorage.SaveAsync(FileCategory.Document, fileName, fileSize, fileStream, cancellationToken);
        if (!saved.IsSuccess)
            return Result<DocumentUploadResponse>.Failure(saved.Error!);

        var document = new Document
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            FileName = Path.GetFileName(fileName),
            FileUrl = saved.Value!,
            ContentType = contentType,
            FileSizeBytes = fileSize,
            EmissionEntryId = emissionEntryId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _documentRepository.AddAsync(document, cancellationToken);
        await _documentRepository.SaveChangesAsync(cancellationToken);

        return Result<DocumentUploadResponse>.Success(new DocumentUploadResponse
        {
            Id = document.Id,
            FileUrl = DownloadUrl(document.Id)
        });
    }

    public async Task<Result<DocumentDownloadResponse>> DownloadAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var document = await _documentRepository.GetByIdAsync(documentId, cancellationToken);
        if (document is null)
            return Result<DocumentDownloadResponse>.Failure("Doküman bulunamadı.");

        var path = _fileStorage.ResolvePath(document.FileUrl);
        if (path is null)
            return Result<DocumentDownloadResponse>.Failure("Dosya depolamada bulunamadı.");

        return Result<DocumentDownloadResponse>.Success(new DocumentDownloadResponse
        {
            CompanyId = document.CompanyId,
            FileName = document.FileName,
            PhysicalPath = path,
            ContentType = string.IsNullOrWhiteSpace(document.ContentType) ? "application/octet-stream" : document.ContentType
        });
    }

    public async Task<Result<IReadOnlyList<DocumentListItem>>> ListByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var docs = await _documentRepository.ListByCompanyAsync(companyId, cancellationToken);
        var items = docs.Select(d => new DocumentListItem
        {
            Id = d.Id,
            FileName = d.FileName,
            FileUrl = DownloadUrl(d.Id),
            ContentType = d.ContentType,
            CreatedAt = d.CreatedAt
        }).ToList();

        return Result<IReadOnlyList<DocumentListItem>>.Success(items);
    }

    private static string DownloadUrl(Guid id) => $"/api/documents/{id}/download";
}
