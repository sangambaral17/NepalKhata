namespace HardwareShopPro.Core.Models;

/// <summary>
/// Represents a product in the hardware shop inventory.
/// </summary>
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public decimal PurchasePrice { get; set; }
    public decimal SellingPrice { get; set; }
    public int Stock { get; set; }
    public int MinStockLevel { get; set; }
    public int? SupplierId { get; set; }
    public DateTime? LastRestockDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public string? SupplierName { get; set; }

    /// <summary>
    /// Returns true if stock is at or below the minimum level.
    /// </summary>
    public bool IsLowStock => Stock <= MinStockLevel;

    /// <summary>
    /// Profit margin percentage.
    /// </summary>
    public decimal ProfitMargin => PurchasePrice > 0
        ? Math.Round((SellingPrice - PurchasePrice) / PurchasePrice * 100, 2)
        : 0;
}
