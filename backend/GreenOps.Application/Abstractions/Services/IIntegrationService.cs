using GreenOps.Application.Common;

namespace GreenOps.Application.Abstractions.Services;

public interface IIntegrationService
{
    Task<Result<ApiKeyResponse>> CreateApiKeyAsync(Guid companyId, string name, CancellationToken cancellationToken = default);
    Task<Result<List<ApiKeyResponse>>> ListApiKeysAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<Result<IntegrationStatusResponse>> GetStatusAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<Result> ReceiveIotMeterDataAsync(string apiKey, IotMeterDataRequest request, CancellationToken cancellationToken = default);
}

public class ApiKeyResponse
{
    public Guid Id { get; set; }
    /// <summary>Düz anahtar sadece oluşturma yanıtında döner; listede null'dur (DB'de hash saklanır).</summary>
    public string? Key { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? LastUsedAt { get; set; }
}

public class IntegrationStatusResponse
{
    public int TotalDevices { get; set; }
    public int OnlineDevices { get; set; }
    public int ActiveApiKeys { get; set; }
    public bool ErpConnected { get; set; }
}

public class IotMeterDataRequest
{
    public string DeviceSerialNumber { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public string Unit { get; set; } = string.Empty;
    public DateTimeOffset ReadingAt { get; set; }
}
