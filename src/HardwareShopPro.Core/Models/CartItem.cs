using CommunityToolkit.Mvvm.ComponentModel;

namespace HardwareShopPro.Core.Models;

/// <summary>
/// Represents an item in the shopping cart for the POS system.
/// </summary>
public partial class CartItem : ObservableObject
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int MaxStock { get; set; }
    public decimal TaxRate { get; set; } = 13.0m; // Default GST 13%

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LineTotal))]
    private int _quantity;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LineTotal))]
    private decimal _discount; // Percentage 0-100

    public decimal LineTotal => (UnitPrice * Quantity) * (1 - (Discount / 100m));

    public void IncrementQuantity()
    {
        if (Quantity < MaxStock)
        {
            Quantity++;
        }
    }

    public void DecrementQuantity()
    {
        if (Quantity > 1)
        {
            Quantity--;
        }
    }

    public void ApplyDiscount(decimal percent)
    {
        if (percent >= 0 && percent <= 100)
        {
            Discount = percent;
        }
    }
}
