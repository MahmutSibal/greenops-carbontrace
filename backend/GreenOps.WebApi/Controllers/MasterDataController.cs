using GreenOps.Application.Abstractions.Repositories;
using GreenOps.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GreenOps.WebApi.Controllers;

/// <summary>Emisyon Kaynakları ve Karbon Katsayıları — Master Data</summary>
[ApiController]
[Route("api")]
[Authorize]
public class MasterDataController : ControllerBase
{
    private readonly IScopeRepository _scopeRepository;
    private readonly IEmissionCategoryRepository _categoryRepository;
    private readonly IConversionFactorRepository _factorRepository;

    public MasterDataController(
        IScopeRepository scopeRepository,
        IEmissionCategoryRepository categoryRepository,
        IConversionFactorRepository factorRepository)
    {
        _scopeRepository = scopeRepository;
        _categoryRepository = categoryRepository;
        _factorRepository = factorRepository;
    }

    /// <summary>GET /api/scopes — Kapsam 1, 2, 3 listesi</summary>
    [HttpGet("scopes")]
    public async Task<IActionResult> GetScopes(CancellationToken ct)
    {
        var scopes = await _scopeRepository.ListAsync(ct);
        return Ok(scopes.Select(s => new { s.Id, s.Number, s.Name, s.Description }));
    }

    /// <summary>GET /api/categories — Tüm emisyon kategorileri</summary>
    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories(CancellationToken ct)
    {
        var categories = await _categoryRepository.ListAsync(ct);
        return Ok(categories.Select(c => new { c.Id, c.Name, c.Unit, c.ScopeId }));
    }

    /// <summary>GET /api/factors — Karbon dönüşüm katsayıları (yıl ve ülke filtreli)</summary>
    [HttpGet("factors")]
    public async Task<IActionResult> GetFactors([FromQuery] int? year, [FromQuery] Guid? categoryId, CancellationToken ct)
    {
        var factors = await _factorRepository.ListAsync(ct);
        var filtered = factors.AsEnumerable();

        if (year.HasValue) filtered = filtered.Where(f => f.Year == year.Value);
        if (categoryId.HasValue) filtered = filtered.Where(f => f.CategoryId == categoryId.Value);

        return Ok(filtered
            .OrderBy(f => f.CategoryId).ThenByDescending(f => f.Year)
            .Select(f => new { f.Id, f.CategoryId, f.Year, f.Factor, f.Source, f.SourceUrl, f.IsVerified, f.IsActive }));
    }

    /// <summary>
    /// POST /api/factors/custom — Katsayı tanımlar/günceller (SuperAdmin).
    /// Katsayılar şirkete özel değil, tüm şirketlerin hesabını etkiler; bu yüzden CompanyAdmin'e kapalı.
    /// </summary>
    [HttpPost("factors/custom")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> CreateCustomFactor([FromBody] CreateCustomFactorRequest request, CancellationToken ct)
    {
        if (request.Factor <= 0 || request.Factor > 100_000m)
            return BadRequest(new { error = "Katsayı 0'dan büyük ve makul bir değer olmalıdır." });
        if (request.Year < 1990 || request.Year > DateTimeOffset.UtcNow.Year + 1)
            return BadRequest(new { error = "Geçersiz yıl." });
        if (await _categoryRepository.GetByIdAsync(request.CategoryId, ct) is null)
            return BadRequest(new { error = "Emisyon kategorisi bulunamadı." });
        // Kaynaksız katsayı girilemez: her hesaplamanın hangi belgeye dayandığı izlenebilir olmalı.
        if (string.IsNullOrWhiteSpace(request.Source) || request.Source.Trim().Length < 10 || request.Source.Length > 500)
            return BadRequest(new { error = "Kaynak (kurum, belge adı/numarası, dönem) 10-500 karakter olarak girilmelidir." });
        if (request.SourceUrl is { Length: > 0 } url && (url.Length > 500 || !Uri.TryCreate(url, UriKind.Absolute, out var u) || u.Scheme != Uri.UriSchemeHttps))
            return BadRequest(new { error = "Kaynak bağlantısı geçerli bir https adresi olmalıdır." });

        // (CategoryId, Year) unique: aynı yıl için ikinci kayıt yerine mevcut katsayı güncellenir.
        // Eski kayıtlar etkilenmez; her emisyon kaydı hesaplandığı katsayı değerini ayrıca saklar.
        var existing = await _factorRepository.GetFactorAsync(request.CategoryId, request.Year, ct);
        if (existing is not null)
        {
            existing.Factor = request.Factor;
            existing.Source = request.Source.Trim();
            existing.SourceUrl = request.SourceUrl;
            existing.IsVerified = request.IsVerified;
            existing.IsActive = true;
            _factorRepository.Update(existing);
            await _factorRepository.SaveChangesAsync(ct);
            return Ok(new { existing.Id });
        }

        var factor = new ConversionFactor
        {
            Id = Guid.NewGuid(),
            CategoryId = request.CategoryId,
            Year = request.Year,
            Factor = request.Factor,
            Source = request.Source.Trim(),
            SourceUrl = request.SourceUrl,
            IsVerified = request.IsVerified,
            IsActive = true
        };

        await _factorRepository.AddAsync(factor, ct);
        await _factorRepository.SaveChangesAsync(ct);
        return Created($"/api/factors/{factor.Id}", new { factor.Id });
    }
}

public class CreateCustomFactorRequest
{
    public Guid CategoryId { get; set; }
    public int Year { get; set; }
    public decimal Factor { get; set; }
    public string Source { get; set; } = string.Empty;
    public string? SourceUrl { get; set; }
    /// <summary>Değer resmi/yayımlanmış bir kaynaktan birebir alındıysa true.</summary>
    public bool IsVerified { get; set; }
}
