using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using HardwareShopPro.Core.Interfaces;
using Serilog;

namespace HardwareShopPro.AI.Services;

/// <summary>
/// Claude AI service using Anthropic Messages API via HttpClient.
/// Implements retry logic with exponential backoff.
/// Fails gracefully when API is unavailable or not configured.
/// </summary>
public class ClaudeAIService : IAIService
{
    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;
    private readonly string _model;
    private readonly int _maxRetries;
    private static readonly ILogger Logger = Log.ForContext<ClaudeAIService>();

    private const string ApiUrl = "https://api.anthropic.com/v1/messages";
    private const string ApiVersion = "2023-06-01";

    public ClaudeAIService(string? apiKey, string model = "claude-sonnet-4-20250514", int maxRetries = 3)
    {
        _apiKey = apiKey;
        _model = model;
        _maxRetries = maxRetries;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

        if (!string.IsNullOrEmpty(_apiKey))
        {
            _httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);
            _httpClient.DefaultRequestHeaders.Add("anthropic-version", ApiVersion);
        }
    }

    public async Task<bool> IsAvailableAsync()
    {
        if (string.IsNullOrEmpty(_apiKey))
            return false;

        try
        {
            // Simple ping — try a minimal request
            var testPayload = new
            {
                model = _model,
                max_tokens = 10,
                messages = new[] { new { role = "user", content = "ping" } }
            };
            var json = JsonSerializer.Serialize(testPayload);
            var response = await _httpClient.PostAsync(ApiUrl,
                new StringContent(json, Encoding.UTF8, "application/json"));
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "AI service availability check failed");
            return false;
        }
    }

    public async Task<SearchCriteria?> SmartSearchAsync(string naturalLanguageQuery)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            Logger.Information("AI not configured — falling back to standard search");
            return null;
        }

        var systemPrompt = @"You are a search query parser for a computer hardware shop inventory system.
Convert the user's natural language query into a JSON object with these fields:
- brand: string or null (e.g., ""Corsair"", ""NVIDIA"")
- category: string or null (e.g., ""RAM"", ""GPU"", ""SSD"", ""CPU"", ""Motherboard"", ""Peripherals"", ""Cables"", ""Monitor"")
- nameContains: string or null (partial product name match)
- maxPrice: number or null (maximum selling price)
- minPrice: number or null (minimum selling price)
- inStockOnly: boolean or null

Respond with ONLY the JSON object, no markdown, no explanation.
Example: ""show all Corsair RAM under 5000"" → {""brand"":""Corsair"",""category"":""RAM"",""maxPrice"":5000}";

        var responseText = await SendMessageWithRetryAsync(systemPrompt, naturalLanguageQuery);
        if (string.IsNullOrEmpty(responseText))
            return null;

        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            return JsonSerializer.Deserialize<SearchCriteria>(responseText, options);
        }
        catch (JsonException ex)
        {
            Logger.Warning(ex, "Failed to parse AI search response: {Response}", responseText);
            return null;
        }
    }

    public async Task<string?> GetSalesInsightsAsync(string salesDataJson)
    {
        if (string.IsNullOrEmpty(_apiKey)) return null;

        var systemPrompt = @"You are a business analytics assistant for a computer hardware shop.
Analyze the sales data and provide brief, actionable insights in 3-5 bullet points.
Focus on trends, top sellers, and recommendations.";

        return await SendMessageWithRetryAsync(systemPrompt, $"Analyze this sales data:\n{salesDataJson}");
    }

    public async Task<string?> GetReorderAlertsAsync(string inventoryDataJson)
    {
        if (string.IsNullOrEmpty(_apiKey)) return null;

        var systemPrompt = @"You are an inventory management assistant for a computer hardware shop.
Based on current stock levels and sales velocity, predict which items need reordering soon.
Provide specific, actionable alerts in 3-5 bullet points.";

        return await SendMessageWithRetryAsync(systemPrompt, $"Analyze this inventory data:\n{inventoryDataJson}");
    }

    /// <summary>
    /// Sends a message to Claude API with exponential backoff retry.
    /// </summary>
    private async Task<string?> SendMessageWithRetryAsync(string systemPrompt, string userMessage)
    {
        for (int attempt = 0; attempt < _maxRetries; attempt++)
        {
            try
            {
                var payload = new
                {
                    model = _model,
                    max_tokens = 1024,
                    system = systemPrompt,
                    messages = new[] { new { role = "user", content = userMessage } }
                };

                var json = JsonSerializer.Serialize(payload);
                var response = await _httpClient.PostAsync(ApiUrl,
                    new StringContent(json, Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(responseJson);
                    var content = doc.RootElement
                        .GetProperty("content")[0]
                        .GetProperty("text")
                        .GetString();
                    return content;
                }

                if ((int)response.StatusCode == 429 || (int)response.StatusCode >= 500)
                {
                    // Retry with exponential backoff
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                    Logger.Warning("AI API returned {StatusCode}, retrying in {Delay}s...",
                        response.StatusCode, delay.TotalSeconds);
                    await Task.Delay(delay);
                    continue;
                }

                Logger.Error("AI API returned {StatusCode}: {Body}",
                    response.StatusCode, await response.Content.ReadAsStringAsync());
                return null;
            }
            catch (TaskCanceledException)
            {
                Logger.Warning("AI API request timed out (attempt {Attempt}/{Max})", attempt + 1, _maxRetries);
            }
            catch (HttpRequestException ex)
            {
                Logger.Warning(ex, "AI API request failed (attempt {Attempt}/{Max})", attempt + 1, _maxRetries);
            }

            if (attempt < _maxRetries - 1)
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                await Task.Delay(delay);
            }
        }

        Logger.Error("AI API request failed after {MaxRetries} attempts", _maxRetries);
        return null;
    }
}
