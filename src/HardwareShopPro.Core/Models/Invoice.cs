using HardwareShopPro.Core.Enums;

namespace HardwareShopPro.Core.Models;

/// <summary>
/// Represents a sales invoice.
/// </summary>
public class Invoice
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public int? CustomerId { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public decimal TotalAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;
    public PaymentMode PaymentMode { get; set; } = PaymentMode.Cash;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public string? CustomerName { get; set; }
    public List<InvoiceItem> Items { get; set; } = new();

    /// <summary>
    /// Net total after discount and including tax.
    /// </summary>
    public decimal NetTotal => TotalAmount - DiscountAmount + TaxAmount;
}
