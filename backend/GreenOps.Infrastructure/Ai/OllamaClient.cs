using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;

namespace GreenOps.Infrastructure.Ai;

public class OllamaOptions
{
    public string BaseUrl { get; init; } = "http://localhost:11434";
    public string VisionModel { get; init; } = "qwen3-vl:8b";
    public string TextModel { get; init; } = "gemma4:12b";
    public int TimeoutSeconds { get; init; } = 240;

    public static OllamaOptions From(IConfiguration configuration)
    {
        var s = configuration.GetSection("Ai");
        return new OllamaOptions
        {
            BaseUrl = string.IsNullOrWhiteSpace(s["BaseUrl"]) ? "http://localhost:11434" : s["BaseUrl"]!,
            VisionModel = string.IsNullOrWhiteSpace(s["VisionModel"]) ? "qwen3-vl:8b" : s["VisionModel"]!,
            TextModel = string.IsNullOrWhiteSpace(s["TextModel"]) ? "gemma4:12b" : s["TextModel"]!,
            TimeoutSeconds = int.TryParse(s["TimeoutSeconds"], out var t) && t > 0 ? t : 240
        };
    }
}

public class OllamaMessage
{
    [JsonPropertyName("role")] public string Role { get; init; } = "user";
    [JsonPropertyName("content")] public string Content { get; init; } = string.Empty;
    [JsonPropertyName("images")][JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string[]? Images { get; init; }
}

public class OllamaUnavailableException : Exception
{
    public OllamaUnavailableException(string message, Exception? inner = null) : base(message, inner) { }
}

/// <summary>Ollama /api/chat için ince istemci (stream kapalı).</summary>
public class OllamaClient
{
    private static readonly JsonSerializerOptions Json = new() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

    private readonly HttpClient _http;

    public OllamaClient(OllamaOptions options)
    {
        Options = options;
        _http = new HttpClient
        {
            BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds)
        };
    }

    public OllamaOptions Options { get; }

    /// <param name="format">"json", bir JSON şeması ya da null (serbest metin).</param>
    public async Task<string> ChatAsync(
        string model,
        IEnumerable<OllamaMessage> messages,
        object? format = null,
        int maxTokens = 800,
        double temperature = 0.2,
        CancellationToken cancellationToken = default)
    {
        var body = new
        {
            model,
            messages,
            stream = false,
            think = false, // gemma4 için düşünme kapalı; qwen3-vl yine düşünebilir, içerik ayrı alandan okunur.
            format,
            keep_alive = "15m",
            options = new { temperature, num_predict = maxTokens }
        };

        HttpResponseMessage response;
        try
        {
            response = await _http.PostAsJsonAsync("api/chat", body, Json, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            throw new OllamaUnavailableException("Yapay zeka servisine (Ollama) ulaşılamadı veya yanıt zaman aşımına uğradı.", ex);
        }

        var text = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new OllamaUnavailableException($"Ollama hata döndü ({(int)response.StatusCode}): {Truncate(text, 200)}");

        var node = JsonNode.Parse(text);
        return node?["message"]?["content"]?.GetValue<string>()?.Trim() ?? string.Empty;
    }

    public async Task<IReadOnlyList<string>> ListModelsAsync(CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(5));
        var node = await _http.GetFromJsonAsync<JsonNode>("api/tags", cts.Token);
        return node?["models"]?.AsArray()
            .Select(m => m?["name"]?.GetValue<string>() ?? "")
            .Where(n => n.Length > 0)
            .ToList() ?? new List<string>();
    }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max] + "…";
}
