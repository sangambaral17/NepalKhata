using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HardwareShopPro.Core.Enums;
using HardwareShopPro.Core.Interfaces;
using HardwareShopPro.Core.Models;
using Serilog;

namespace HardwareShopPro.UI.ViewModels;

public partial class BillingViewModel : ViewModelBase
{
    private readonly IProductRepository _productRepo;
    private readonly IInvoiceRepository _invoiceRepo;
    private readonly ICustomerRepository _customerRepo;
    private static readonly ILogger Logger = Log.ForContext<BillingViewModel>();

    // ─── Search & Cart ───────────────────────────────────────────────────
    [ObservableProperty] private string _searchQuery = string.Empty;
    [ObservableProperty] private ObservableCollection<Product> _searchResults = new();
    [ObservableProperty] private ObservableCollection<CartItem> _cartItems = new();
    [ObservableProperty] private CartItem? _selectedCartItem;

    // ─── Customer ────────────────────────────────────────────────────────
    [ObservableProperty] private Customer? _selectedCustomer;
    [ObservableProperty] private string _customerSearchQuery = string.Empty;
    [ObservableProperty] private ObservableCollection<Customer> _customerResults = new();

    // ─── Totals ──────────────────────────────────────────────────────────
    [ObservableProperty] private decimal _subTotal;
    [ObservableProperty] private decimal _totalDiscount;
    [ObservableProperty] private decimal _totalTax;
    [ObservableProperty] private decimal _grandTotal;

    // ─── Payment ─────────────────────────────────────────────────────────
    [ObservableProperty] private PaymentMode _paymentMode = PaymentMode.Cash;
    [ObservableProperty] private decimal _amountReceived;
    [ObservableProperty] private decimal _changeAmount;
    [ObservableProperty] private string _invoiceNumber = "Generating...";

    [ObservableProperty] private bool _isProcessing;

    public BillingViewModel(
        IProductRepository productRepo,
        IInvoiceRepository invoiceRepo,
        ICustomerRepository customerRepo)
    {
        _productRepo = productRepo;
        _invoiceRepo = invoiceRepo;
        _customerRepo = customerRepo;

        CartItems.CollectionChanged += (s, e) => RecalculateTotals();
    }

    public override async Task LoadAsync()
    {
        InvoiceNumber = await _invoiceRepo.GenerateNextInvoiceNumberAsync();
        // Load default customer or walk-in logic if needed
    }

    // ─── Commands ────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task SearchProducts(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            SearchResults.Clear();
            return;
        }

        var results = await _productRepo.SearchAsync(query);
        SearchResults = new ObservableCollection<Product>(results);
    }

    [RelayCommand]
    private void AddToCart(Product product)
    {
        if (product.Stock <= 0)
        {
            ErrorMessage = $"Product '{product.Name}' is out of stock!";
            return;
        }

        var existingItem = CartItems.FirstOrDefault(c => c.ProductId == product.Id);
        if (existingItem != null)
        {
            if (existingItem.Quantity < existingItem.MaxStock)
            {
                existingItem.Quantity++;
                RecalculateTotals();
            }
            else
            {
                ErrorMessage = $"Cannot add more. Max stock available: {existingItem.MaxStock}";
            }
        }
        else
        {
            var newItem = new CartItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                SKU = product.SKU,
                UnitPrice = product.SellingPrice,
                MaxStock = product.Stock,
                Quantity = 1,
                Discount = 0
            };
            // Re-subscribe to property changes for live total updates
            newItem.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(CartItem.LineTotal))
                    RecalculateTotals();
            };
            CartItems.Add(newItem);
        }
        
        SearchQuery = string.Empty;
        SearchResults.Clear();
        ErrorMessage = null;
    }

    [RelayCommand]
    private void RemoveFromCart(CartItem item)
    {
        CartItems.Remove(item);
    }

    [RelayCommand]
    private void ClearCart()
    {
        if (CartItems.Count > 0)
        {
            if (MessageBox.Show("Are you sure you want to clear the cart?", "Clear Cart", 
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                CartItems.Clear();
                SelectedCustomer = null;
                AmountReceived = 0;
            }
        }
    }

    [RelayCommand]
    private async Task GenerateInvoice()
    {
        if (CartItems.Count == 0)
        {
            ErrorMessage = "Cart is empty!";
            return;
        }

        if (IsProcessing) return;

        IsProcessing = true;
        try
        {
            var invoice = new Invoice
            {
                InvoiceNumber = InvoiceNumber,
                CustomerId = SelectedCustomer?.Id,
                Date = DateTime.UtcNow,
                TotalAmount = GrandTotal, 
                TaxAmount = TotalTax,
                DiscountAmount = TotalDiscount,
                PaymentStatus = PaymentStatus.Paid,
                PaymentMode = PaymentMode,
                Items = CartItems.Select(c => new InvoiceItem
                {
                    ProductId = c.ProductId,
                    Quantity = c.Quantity,
                    Price = c.UnitPrice,
                    Discount = c.Discount // Storing percentage here logic might need adjustment based on InvoiceItem model interpretation, assuming simple value mapping for now.
                    // Actually InvoiceItem.Discount is usually amount.
                    // Let's fix calculation: discount amount per item = (Price * Quantity) * (Percent / 100)
                }).ToList()
            };

            // Fix Discount mapping: model expects amount, cart uses percentage
            foreach (var item in invoice.Items)
            {
                var cartItem = CartItems.First(c => c.ProductId == item.ProductId);
                // Calculate discount amount for this line
                var lineGross = cartItem.UnitPrice * cartItem.Quantity;
                item.Discount = lineGross * (cartItem.Discount / 100m);
            }

            await _invoiceRepo.AddAsync(invoice);

            MessageBox.Show($"Invoice {invoice.InvoiceNumber} generated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            
            CartItems.Clear();
            SelectedCustomer = null;
            AmountReceived = 0;
            InvoiceNumber = await _invoiceRepo.GenerateNextInvoiceNumberAsync();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to generate invoice");
            ErrorMessage = "Failed to generate invoice. Please try again.";
        }
        finally
        {
            IsProcessing = false;
        }
    }

    [RelayCommand]
    private async Task SearchCustomers(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            CustomerResults.Clear();
            return;
        }
        
        // Simple search logic
        var allCustomers = await _customerRepo.GetAllAsync();
        var filtered = allCustomers.Where(c => 
            c.Name.Contains(query, StringComparison.OrdinalIgnoreCase) || 
            c.Phone.Contains(query));
            
        CustomerResults = new ObservableCollection<Customer>(filtered);
    }

    [RelayCommand]
    private void SelectCustomer(Customer customer)
    {
        SelectedCustomer = customer;
        CustomerResults.Clear();
        CustomerSearchQuery = string.Empty;
    }

    // ─── Helpers ─────────────────────────────────────────────────────────

    private void RecalculateTotals()
    {
        SubTotal = CartItems.Sum(i => i.UnitPrice * i.Quantity);
        
        // Calculate total discount amount from percentages
        TotalDiscount = CartItems.Sum(i => (i.UnitPrice * i.Quantity) * (i.Discount / 100m));
        
        // Simplified Tax logic: (SubTotal - Discount) * 13%
        // Or per-item tax? Specification says "Tax/GST row". 
        // Assuming global 13% on net total for simplicity unless items have specific tax rates.
        // CartItem has TaxRate property, let's use that.
        
        TotalTax = CartItems.Sum(i => i.LineTotal * (i.TaxRate / 100m));
        
        GrandTotal = (SubTotal - TotalDiscount) + TotalTax;
        
        CalculateChange();
    }

    partial void OnAmountReceivedChanged(decimal value)
    {
        CalculateChange();
    }

    private void CalculateChange()
    {
        if (PaymentMode == PaymentMode.Cash)
        {
            ChangeAmount = AmountReceived - GrandTotal;
        }
        else
        {
            ChangeAmount = 0;
        }
    }
}
