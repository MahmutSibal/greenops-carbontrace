using GreenOps.Application.Abstractions.Repositories;
using GreenOps.Application.Abstractions.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GreenOps.WebApi.Controllers;

[Route("api/documents")]
[Authorize]
public class DocumentsController : ApiControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly IAiService _aiService;

    public DocumentsController(IDocumentService documentService, IAiService aiService)
    {
        _documentService = documentService;
        _aiService = aiService;
    }

    /// <summary>GET /api/documents — Şirketin belgelerini listeler</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid? companyId, CancellationToken ct)
    {
        if (!TryResolveCompany(companyId, out var cid)) return CompanyForbidden();
        var result = await _documentService.ListByCompanyAsync(cid, ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

    /// <summary>POST /api/documents/upload — Fatura, irsaliye, sayaç fotoğrafı yükler (en fazla 10 MB)</summary>
    [HttpPost("upload")]
    [RequestSizeLimit(11 * 1024 * 1024)]
    public async Task<IActionResult> Upload(IFormFile file, [FromQuery] Guid? companyId, [FromQuery] Guid? emissionEntryId, CancellationToken ct)
    {
        if (!TryResolveCompany(companyId, out var cid)) return CompanyForbidden();
        if (file is null || file.Length == 0) return BadRequest(new { error = "Dosya seçilmedi." });

        await using var stream = file.OpenReadStream();
        var result = await _documentService.UploadAsync(cid, file.FileName, file.ContentType, file.Length, stream, emissionEntryId, ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

    /// <summary>GET /api/documents/{id}/download — Evrakı indirir</summary>
    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> Download(Guid id, CancellationToken ct)
    {
        var result = await _documentService.DownloadAsync(id, ct);
        if (!result.IsSuccess || !CanAccessCompany(result.Value!.CompanyId))
            return NotFoundError("Doküman bulunamadı.");

        var doc = result.Value;
        return PhysicalFile(doc.PhysicalPath, doc.ContentType, doc.FileName);
    }

    /// <summary>POST /api/documents/ocr-scan — Yüklenmiş fatura görselinden AI ile tüketim bilgisini okur</summary>
    [HttpPost("ocr-scan")]
    [EnableRateLimiting(RateLimitPolicies.Ai)]
    public async Task<IActionResult> OcrScan([FromBody] OcrScanRequest request, CancellationToken ct)
    {
        var doc = await _documentService.DownloadAsync(request.DocumentId, ct);
        if (!doc.IsSuccess || !CanAccessCompany(doc.Value!.CompanyId)) return NotFoundError("Doküman bulunamadı.");

        var ext = Path.GetExtension(doc.Value.PhysicalPath).ToLowerInvariant();
        if (ext is not (".png" or ".jpg" or ".jpeg" or ".webp"))
            return BadRequest(new { error = "AI okuma şimdilik sadece görsel (png/jpg/webp) belgelerde çalışır." });

        var bytes = await System.IO.File.ReadAllBytesAsync(doc.Value.PhysicalPath, ct);
        var result = await _aiService.ExtractInvoiceAsync(doc.Value.CompanyId, bytes, ct);
        if (!result.IsSuccess) return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = result.Error });
        return Ok(result.Value);
    }
}

public class OcrScanRequest { public Guid DocumentId { get; set; } }
