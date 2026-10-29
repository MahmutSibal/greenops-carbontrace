using GreenOps.Application.Abstractions.Repositories;
using GreenOps.Application.Abstractions.Services;
using GreenOps.Application.Common;
using GreenOps.Domain.Entities;
using GreenOps.Infrastructure.Persistence;
using GreenOps.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace GreenOps.Infrastructure.Services;

public class IntegrationService : IIntegrationService
{
    private readonly IApiKeyRepository _apiKeyRepository;
    private readonly IIotDeviceRepository _iotDeviceRepository;
    private readonly IIotMeterReadingRepository _meterReadingRepository;
    private readonly AppDbContext _dbContext;

    public IntegrationService(
        IApiKeyRepository apiKeyRepository,
        IIotDeviceRepository iotDeviceRepository,
        IIotMeterReadingRepository meterReadingRepository,
        AppDbContext dbContext)
    {
        _apiKeyRepository = apiKeyRepository;
        _iotDeviceRepository = iotDeviceRepository;
        _meterReadingRepository = meterReadingRepository;
        _dbContext = dbContext;
    }

    public async Task<Result<ApiKeyResponse>> CreateApiKeyAsync(Guid companyId, string name, CancellationToken cancellationToken = default)
    {
        name = name?.Trim() ?? string.Empty;
        if (name.Length is < 2 or > 200)
            return Result<ApiKeyResponse>.Failure("Anahtar adı 2-200 karakter olmalıdır.");

        var keyValue = $"gops_{TokenHasher.NewUrlSafeToken()}";
        var apiKey = new ApiKey
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Name = name,
            Key = TokenHasher.Sha256(keyValue), // Düz anahtar saklanmaz, sadece bu yanıtta bir kez gösterilir.
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _apiKeyRepository.AddAsync(apiKey, cancellationToken);
        await _apiKeyRepository.SaveChangesAsync(cancellationToken);

        return Result<ApiKeyResponse>.Success(new ApiKeyResponse
        {
            Id = apiKey.Id,
            Key = keyValue,
            Name = apiKey.Name,
            IsActive = true,
            CreatedAt = apiKey.CreatedAt
        });
    }

    public async Task<Result<List<ApiKeyResponse>>> ListApiKeysAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var keys = await _apiKeyRepository.ListByCompanyAsync(companyId, cancellationToken);
        var result = keys.Select(k => new ApiKeyResponse
        {
            Id = k.Id,
            Name = k.Name,
            IsActive = k.IsActive,
            CreatedAt = k.CreatedAt,
            LastUsedAt = k.LastUsedAt
        }).ToList();

        return Result<List<ApiKeyResponse>>.Success(result);
    }

    public async Task<Result<IntegrationStatusResponse>> GetStatusAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var devices = _dbContext.IotDevices.Where(d => d.Facility!.CompanyId == companyId);
        var onlineSince = DateTimeOffset.UtcNow.AddMinutes(-30);

        return Result<IntegrationStatusResponse>.Success(new IntegrationStatusResponse
        {
            TotalDevices = await devices.CountAsync(cancellationToken),
            OnlineDevices = await devices.CountAsync(d => d.IsActive && d.LastSeenAt > onlineSince, cancellationToken),
            ActiveApiKeys = await _dbContext.ApiKeys.CountAsync(k => k.CompanyId == companyId && k.IsActive, cancellationToken),
            ErpConnected = false // ERP entegrasyonu henüz yok.
        });
    }

    public async Task<Result> ReceiveIotMeterDataAsync(string apiKey, IotMeterDataRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            return Result.Failure("API anahtarı gerekli.");

        var key = await _apiKeyRepository.GetByKeyAsync(TokenHasher.Sha256(apiKey), cancellationToken);
        if (key is null)
            return Result.Failure("Geçersiz API anahtarı.");

        if (request.Value < 0 || request.Value > 1_000_000_000m)
            return Result.Failure("Geçersiz sayaç değeri.");
        if (string.IsNullOrWhiteSpace(request.Unit) || request.Unit.Length > 20)
            return Result.Failure("Birim 1-20 karakter olmalıdır.");
        if (request.ReadingAt > DateTimeOffset.UtcNow.AddMinutes(5))
            return Result.Failure("Okuma zamanı gelecekte olamaz.");

        // Cihaz, anahtarın ait olduğu şirketin bir tesisinde olmalı.
        var device = await _dbContext.IotDevices
            .FirstOrDefaultAsync(d => d.SerialNumber == request.DeviceSerialNumber
                                      && d.IsActive
                                      && d.Facility!.CompanyId == key.CompanyId, cancellationToken);
        if (device is null)
            return Result.Failure("IoT cihazı bulunamadı veya aktif değil.");

        var reading = new IotMeterReading
        {
            Id = Guid.NewGuid(),
            IotDeviceId = device.Id,
            Value = request.Value,
            Unit = request.Unit,
            ReadingTime = request.ReadingAt,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _meterReadingRepository.AddAsync(reading, cancellationToken);
        device.LastSeenAt = DateTimeOffset.UtcNow;
        _iotDeviceRepository.Update(device);
        key.LastUsedAt = DateTimeOffset.UtcNow;
        await _meterReadingRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
