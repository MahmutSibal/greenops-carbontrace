using GreenOps.Application.Common;

namespace GreenOps.Application.Abstractions.Services;

/// <summary>
/// Yerel Ollama modelleriyle çalışan yapay zeka yetenekleri.
/// AI çıktıları her zaman öneridir: kayıtlar kullanıcı onayı olmadan oluşturulmaz veya değiştirilmez.
/// </summary>
public interface IAiService
{
    /// <summary>Fatura/sayaç görselinden tüketim bilgisini çıkarır (görsel model OCR → metin model yapılandırma).</summary>
    Task<Result<InvoiceExtraction>> ExtractInvoiceAsync(Guid companyId, byte[] image, CancellationToken cancellationToken = default);

    /// <summary>Şirketin emisyon verisinden tahmin, anomali ve öneri üretir.</summary>
    Task<Result<EmissionInsights>> GetInsightsAsync(Guid companyId, bool refresh, CancellationToken cancellationToken = default);

    /// <summary>Şirket verisine dayanan sohbet asistanı.</summary>
    Task<Result<string>> ChatAsync(Guid companyId, string userName, IReadOnlyList<ChatTurn> history, string message, CancellationToken cancellationToken = default);

    Task<AiStatus> GetStatusAsync(CancellationToken cancellationToken = default);
}

public class InvoiceExtraction
{
    public decimal? Consumption { get; set; }
    public string? Unit { get; set; }
    public DateTimeOffset? PeriodStart { get; set; }
    public DateTimeOffset? PeriodEnd { get; set; }
    public string? Provider { get; set; }
    public string? DocumentType { get; set; }
    public decimal Confidence { get; set; }
    /// <summary>Birim eşleşen emisyon kategorisi (ör. kWh → Electricity).</summary>
    public Guid? SuggestedCategoryId { get; set; }
    public string? SuggestedCategoryName { get; set; }
    public string RawText { get; set; } = string.Empty;
    public List<string> Warnings { get; set; } = new();
}

public class EmissionInsights
{
    public string Summary { get; set; } = string.Empty;
    public List<string> Recommendations { get; set; } = new();
    /// <summary>Doğrusal eğilimle hesaplanan gelecek ay tahmini (kgCO2e); veri yetersizse null.</summary>
    public decimal? ForecastNextMonth { get; set; }
    public string? ForecastMonth { get; set; }
    public string Trend { get; set; } = "stable"; // up | down | stable | insufficient
    public List<EmissionAnomaly> Anomalies { get; set; } = new();
    public DateTimeOffset GeneratedAt { get; set; }
    public string Model { get; set; } = string.Empty;
    public bool AiAvailable { get; set; }
}

public class EmissionAnomaly
{
    public Guid EntryId { get; set; }
    public string FacilityName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public decimal TypicalValue { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class ChatTurn
{
    public string Role { get; set; } = "user"; // user | assistant
    public string Content { get; set; } = string.Empty;
}

public class AiStatus
{
    public bool Available { get; set; }
    public string VisionModel { get; set; } = string.Empty;
    public string TextModel { get; set; } = string.Empty;
    public bool VisionModelInstalled { get; set; }
    public bool TextModelInstalled { get; set; }
    public string? Error { get; set; }
}
