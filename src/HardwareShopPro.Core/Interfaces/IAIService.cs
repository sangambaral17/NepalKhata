namespace HardwareShopPro.Core.Interfaces;

/// <summary>
/// Search criteria returned by AI smart search — used to filter products.
/// </summary>
public class SearchCriteria
{
    public string? Brand { get; set; }
    public string? Category { get; set; }
    public string? NameContains { get; set; }
    public decimal? MaxPrice { get; set; }
    public decimal? MinPrice { get; set; }
    public bool? InStockOnly { get; set; }
}

/// <summary>
/// AI service interface for Claude API integration.
/// </summary>
public interface IAIService
{
    /// <summary>
    /// Converts a natural language query into structured search criteria.
    /// Example: "show all Corsair RAM under 5000" → { Brand: "Corsair", Category: "RAM", MaxPrice: 5000 }
    /// </summary>
    Task<SearchCriteria?> SmartSearchAsync(string naturalLanguageQuery);

    /// <summary>
    /// Generates sales insights in natural language.
    /// </summary>
    Task<string?> GetSalesInsightsAsync(string salesDataJson);

    /// <summary>
    /// Predicts which products will need reordering soon.
    /// </summary>
    Task<string?> GetReorderAlertsAsync(string inventoryDataJson);

    /// <summary>
    /// Returns true if the AI service is configured and reachable.
    /// </summary>
    Task<bool> IsAvailableAsync();
}
