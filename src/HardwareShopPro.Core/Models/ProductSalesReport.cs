namespace HardwareShopPro.Core.Models;

/// <summary>
/// Data model for product sales reporting.
/// </summary>
public class ProductSalesReport
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int TotalQuantity { get; set; }
    public decimal TotalRevenue { get; set; }
    
    // For charts
    public string Label => ProductName;
    public double Value => (double)TotalRevenue;
}
