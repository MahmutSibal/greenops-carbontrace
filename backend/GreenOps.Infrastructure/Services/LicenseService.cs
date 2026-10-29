using System.Security.Cryptography;
using GreenOps.Application.Abstractions.Repositories;
using GreenOps.Application.Abstractions.Services;
using GreenOps.Application.Common;
using GreenOps.Domain.Entities;

namespace GreenOps.Infrastructure.Services;

public class LicenseService : ILicenseService
{
    private readonly ILicenseRepository _licenseRepository;

    public LicenseService(ILicenseRepository licenseRepository)
    {
        _licenseRepository = licenseRepository;
    }

    public async Task<Result<LicenseCreateResponse>> CreateAsync(CreateLicenseRequest request, CancellationToken cancellationToken = default)
    {
        var key = GenerateLicenseKey();

        var license = new License
        {
            Id = Guid.NewGuid(),
            CompanyId = request.CompanyId,
            LicenseKey = key,
            PlanName = request.PlanName,
            MaxFacilities = request.MaxFacilities,
            MaxUsers = request.MaxUsers,
            IsActive = false,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(request.DurationDays)
        };

        await _licenseRepository.AddAsync(license, cancellationToken);
        await _licenseRepository.SaveChangesAsync(cancellationToken);

        return Result<LicenseCreateResponse>.Success(new LicenseCreateResponse
        {
            Id = license.Id,
            LicenseKey = license.LicenseKey,
            PlanName = license.PlanName,
            ExpiresAt = license.ExpiresAt
        });
    }

    public async Task<Result> ActivateAsync(ActivateLicenseRequest request, CancellationToken cancellationToken = default)
    {
        var license = await _licenseRepository.GetByKeyAsync(request.LicenseKey, cancellationToken);
        if (license is null)
            return Result.Failure("Lisans anahtarı bulunamadı.");

        if (license.IsActive)
            return Result.Failure("Bu lisans zaten aktif.");

        license.MacAddress = request.MacAddress;
        license.IpAddress = request.IpAddress;
        license.IsActive = true;
        license.ActivatedAt = DateTimeOffset.UtcNow;

        _licenseRepository.Update(license);
        await _licenseRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result<LicenseStatusResponse>> GetStatusAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var license = await _licenseRepository.GetActiveByCompanyAsync(companyId, cancellationToken);
        if (license is null)
            return Result<LicenseStatusResponse>.Failure("Aktif lisans bulunamadı.");

        return Result<LicenseStatusResponse>.Success(new LicenseStatusResponse
        {
            IsActive = license.IsActive,
            LicenseKey = license.LicenseKey,
            PlanName = license.PlanName,
            MaxFacilities = license.MaxFacilities,
            MaxUsers = license.MaxUsers,
            MacAddress = license.MacAddress,
            ExpiresAt = license.ExpiresAt
        });
    }

    public async Task<Result<List<LicenseListItem>>> GetAllByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var licenses = await _licenseRepository.GetAllByCompanyAsync(companyId, cancellationToken);

        var items = licenses.Select(l => new LicenseListItem
        {
            Id = l.Id,
            LicenseKey = l.LicenseKey,
            PlanName = l.PlanName,
            IsActive = l.IsActive,
            MaxFacilities = l.MaxFacilities,
            MaxUsers = l.MaxUsers,
            CreatedAt = l.CreatedAt,
            ExpiresAt = l.ExpiresAt
        }).ToList();

        return Result<List<LicenseListItem>>.Success(items);
    }

    private static string GenerateLicenseKey()
    {
        var segments = new string[4];
        var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        for (int i = 0; i < 4; i++)
        {
            var segment = new char[4];
            for (int j = 0; j < 4; j++)
                segment[j] = chars[RandomNumberGenerator.GetInt32(chars.Length)];
            segments[i] = new string(segment);
        }
        return string.Join("-", segments);
    }
}
