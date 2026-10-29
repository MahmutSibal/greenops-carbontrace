using GreenOps.Application.Abstractions.Services;

namespace GreenOps.WebApi.Security;

/// <summary>
/// Uygulama açılınca metin modelini arka planda belleğe yükler; ilk fatura okuma/asistan isteği
/// model yükleme süresini beklemez. Ollama kapalıysa sadece uyarı yazar, uygulama çalışmaya devam eder.
/// </summary>
public class AiWarmUpService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AiWarmUpService> _logger;

    public AiWarmUpService(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<AiWarmUpService> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_configuration.GetValue("Ai:WarmUpOnStartup", true))
            return;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            await scope.ServiceProvider.GetRequiredService<IAiService>().WarmUpAsync(stoppingToken);
            _logger.LogInformation("AI metin modeli belleğe yüklendi.");
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI modeli ısıtılamadı; ilk istek daha yavaş olabilir.");
        }
    }
}
