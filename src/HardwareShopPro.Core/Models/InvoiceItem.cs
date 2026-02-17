namespace HardwareShopPro.Core.Models;

/// <summary>
/// Represents a single line item on an invoice.
/// </summary>
public class InvoiceItem
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal Discount { get; set; }

    // Navigation
    public string? ProductName { get; set; }

    /// <summary>
    /// Line total after discount.
    /// </summary>
    public decimal LineTotal => (Price * Quantity) - Discount;
}
