using System.Globalization;
using GreenOps.Application.EmissionEntries;
using GreenOps.Application.EmissionEntries.Commands;
using GreenOps.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GreenOps.WebApi.Controllers;

[Route("api/emissions")]
[Authorize]
public class EmissionEntriesController : ApiControllerBase
{
    private const int MaxBulkRows = 5_000;

    private readonly IMediator _mediator;
    private readonly IEmissionEntryService _emissionEntryService;

    public EmissionEntriesController(IMediator mediator, IEmissionEntryService emissionEntryService)
    {
        _mediator = mediator;
        _emissionEntryService = emissionEntryService;
    }

    /// <summary>POST /api/emissions/entry — Tekil veri girişi (hesaplama motoru tetiklenir)</summary>
    [HttpPost("entry")]
    public async Task<IActionResult> Create([FromBody] CreateEmissionEntryCommand command, CancellationToken ct)
    {
        if (CurrentUserId is not { } userId) return Unauthorized();
        command.CreatedByUserId = userId;
        command.CompanyScope = CompanyScope;

        var result = await _mediator.Send(command, ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return CreatedAtAction(nameof(GetById), new { id = result.Value }, new { id = result.Value });
    }

    /// <summary>
    /// POST /api/emissions/bulk-upload — CSV ile toplu veri aktarımı.
    /// Sütunlar: facilityId,categoryId,value[,evidenceUrl[,periodStart(yyyy-MM-dd)[,periodEnd]]]
    /// </summary>
    [HttpPost("bulk-upload")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> BulkUpload(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0) return BadRequest(new { error = "Dosya seçilmedi." });
        if (!string.Equals(Path.GetExtension(file.FileName), ".csv", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "Sadece .csv dosyası yüklenebilir." });
        if (CurrentUserId is not { } userId) return Unauthorized();

        var rowsProcessed = 0;
        var errors = new List<string>();

        using var reader = new StreamReader(file.OpenReadStream());
        var headerLine = await reader.ReadLineAsync(ct);
        if (headerLine is null) return BadRequest(new { error = "Dosya boş." });

        var lineNumber = 1;
        string? line;
        while ((line = await reader.ReadLineAsync(ct)) is not null)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (lineNumber - 1 > MaxBulkRows)
            {
                errors.Add($"En fazla {MaxBulkRows} satır işlenir; kalan satırlar atlandı.");
                break;
            }

            var parts = line.Split(',');
            if (parts.Length < 3)
            {
                errors.Add($"Satır {lineNumber}: Yetersiz sütun.");
                continue;
            }

            if (!Guid.TryParse(parts[0].Trim(), out var facilityId) ||
                !Guid.TryParse(parts[1].Trim(), out var categoryId) ||
                !decimal.TryParse(parts[2].Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
            {
                errors.Add($"Satır {lineNumber}: Geçersiz veri formatı.");
                continue;
            }

            DateTimeOffset? periodStart = null, periodEnd = null;
            if ((parts.Length > 4 && !TryParseDate(parts[4], out periodStart)) ||
                (parts.Length > 5 && !TryParseDate(parts[5], out periodEnd)))
            {
                errors.Add($"Satır {lineNumber}: Dönem tarihi yyyy-MM-dd olmalıdır.");
                continue;
            }

            var evidenceUrl = parts.Length > 3 && !string.IsNullOrWhiteSpace(parts[3]) ? parts[3].Trim() : null;
            if (evidenceUrl?.Length > 500)
            {
                errors.Add($"Satır {lineNumber}: Kanıt URL'si en fazla 500 karakter olabilir.");
                continue;
            }

            var entry = new EmissionEntry
            {
                FacilityId = facilityId,
                CategoryId = categoryId,
                Value = value,
                EvidenceUrl = evidenceUrl,
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,
                CreatedByUserId = userId
            };

            var result = await _emissionEntryService.CreateAsync(entry, CompanyScope, ct);
            if (result.IsSuccess)
                rowsProcessed++;
            else
                errors.Add($"Satır {lineNumber}: {result.Error}");
        }

        return Ok(new { message = "Toplu veri aktarımı tamamlandı.", rowsProcessed, errors });
    }

    /// <summary>GET /api/emissions — Şirketin verilerini sayfalayarak ve filtreleyerek listeler</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? companyId,
        [FromQuery] Guid? facilityId,
        [FromQuery] Guid? categoryId,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        Guid? scope;
        if (IsSuperAdmin && (companyId is null || companyId == Guid.Empty))
            scope = null; // SuperAdmin tüm şirketler
        else if (TryResolveCompany(companyId, out var cid))
            scope = cid;
        else
            return CompanyForbidden();

        var result = await _emissionEntryService.GetPagedAsync(new EmissionQuery
        {
            CompanyId = scope, FacilityId = facilityId, CategoryId = categoryId,
            From = from, To = to, Page = page, PageSize = pageSize
        }, ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });

        var paged = result.Value!;
        return Ok(new
        {
            totalCount = paged.TotalCount,
            page = paged.Page,
            pageSize = paged.PageSize,
            items = paged.Items.Select(e => new
            {
                e.Id, e.CompanyId, e.FacilityId, FacilityName = e.Facility?.Name,
                e.CategoryId, CategoryName = e.Category?.Name, Unit = e.Unit ?? e.Category?.Unit,
                e.Value, e.FactorValue, e.CalculationResult, e.EvidenceUrl,
                e.PeriodStart, e.PeriodEnd,
                Status = e.Status.ToString(), e.CreatedAt, e.ApprovedAt
            })
        });
    }

    /// <summary>GET /api/emissions/{id} — Emisyon detayı, katsayısı, formülü</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var e = await FindAccessibleAsync(id, ct);
        if (e is null) return NotFoundError("Emisyon kaydı bulunamadı.");

        return Ok(new
        {
            e.Id, e.CompanyId, e.FacilityId, FacilityName = e.Facility?.Name,
            e.CategoryId, CategoryName = e.Category?.Name, CategoryUnit = e.Unit ?? e.Category?.Unit,
            e.Value, e.CalculationResult, e.ConversionFactorId, e.FactorValue,
            FactorYear = e.ConversionFactor?.Year,
            FactorSource = e.ConversionFactor?.Source,
            FactorSourceUrl = e.ConversionFactor?.SourceUrl,
            FactorVerified = e.ConversionFactor?.IsVerified ?? false,
            Formula = $"{e.Value} * {e.FactorValue?.ToString() ?? "factor"} = {e.CalculationResult}",
            e.PeriodStart, e.PeriodEnd,
            e.EvidenceUrl, Status = e.Status.ToString(),
            e.RejectionReason, e.CreatedAt, e.CreatedByUserId, e.UpdatedAt, e.ApprovedAt, e.ApprovedByUserId,
            e.RejectedAt, e.RejectedByUserId
        });
    }

    /// <summary>PUT /api/emissions/{id} — Onaylanmamış veriyi günceller (kaydı giren veya Manager+)</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEmissionRequest request, CancellationToken ct)
    {
        var entry = await FindAccessibleAsync(id, ct);
        if (entry is null) return NotFoundError("Emisyon kaydı bulunamadı.");
        if (!CanModify(entry)) return Forbid();

        var result = await _emissionEntryService.UpdateAsync(id, request.Value, request.CategoryId, ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(new { message = "Emisyon kaydı güncellendi." });
    }

    /// <summary>DELETE /api/emissions/{id} — Onaylanmamış hatalı girişi soft-delete yapar</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var entry = await FindAccessibleAsync(id, ct);
        if (entry is null) return NotFoundError("Emisyon kaydı bulunamadı.");
        if (!CanModify(entry)) return Forbid();

        var result = await _emissionEntryService.SoftDeleteAsync(id, ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(new { message = "Emisyon kaydı silindi." });
    }

    /// <summary>PUT /api/emissions/{id}/approve — (Denetçi) Veriyi onaylar; kendi kaydını onaylayamaz</summary>
    [HttpPut("{id:guid}/approve")]
    [Authorize(Roles = ManagerRoles)]
    public async Task<IActionResult> Approve(Guid id, CancellationToken ct)
    {
        if (CurrentUserId is not { } userId) return Unauthorized();
        if (await FindAccessibleAsync(id, ct) is null) return NotFoundError("Emisyon kaydı bulunamadı.");

        var result = await _emissionEntryService.ApproveAsync(id, userId, allowSelfApproval: IsSuperAdmin, ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(new { message = "Emisyon kaydı onaylandı." });
    }

    /// <summary>PUT /api/emissions/{id}/reject — (Denetçi) Veriyi reddeder</summary>
    [HttpPut("{id:guid}/reject")]
    [Authorize(Roles = ManagerRoles)]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectEmissionRequest request, CancellationToken ct)
    {
        if (CurrentUserId is not { } userId) return Unauthorized();
        if (await FindAccessibleAsync(id, ct) is null) return NotFoundError("Emisyon kaydı bulunamadı.");

        var result = await _emissionEntryService.RejectAsync(id, request.Reason, userId, ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(new { message = "Emisyon kaydı reddedildi.", reason = request.Reason });
    }

    private async Task<EmissionEntry?> FindAccessibleAsync(Guid id, CancellationToken ct)
    {
        var result = await _emissionEntryService.GetByIdAsync(id, ct);
        return result.IsSuccess && CanAccessCompany(result.Value!.CompanyId) ? result.Value : null;
    }

    private bool CanModify(EmissionEntry entry) =>
        IsAtLeast(UserRole.Manager) || entry.CreatedByUserId == CurrentUserId;

    private static bool TryParseDate(string raw, out DateTimeOffset? date)
    {
        date = null;
        if (string.IsNullOrWhiteSpace(raw)) return true;
        if (!DateTime.TryParseExact(raw.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
            return false;
        date = new DateTimeOffset(d, TimeSpan.Zero);
        return true;
    }
}

public class UpdateEmissionRequest { public decimal? Value { get; set; } public Guid? CategoryId { get; set; } }
public class RejectEmissionRequest { public string Reason { get; set; } = string.Empty; }
