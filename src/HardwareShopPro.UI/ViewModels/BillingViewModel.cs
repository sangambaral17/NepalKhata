using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HardwareShopPro.Core.Enums;
using HardwareShopPro.Core.Interfaces;
using HardwareShopPro.Core.Models;
using HardwareShopPro.Core.Services;
using HardwareShopPro.UI.Services;
using Serilog;

namespace HardwareShopPro.UI.ViewModels;

public partial class BillingViewModel : ViewModelBase
{
    private readonly IProductRepository _productRepo;
    private readonly IInvoiceRepository _invoiceRepo;
    private readonly ICustomerRepository _customerRepo;
    private readonly AppConfigService _configService;
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

    // ─── Quick Add Customer Dialog ───────────────────────────────────────
    [ObservableProperty] private bool _isQuickAddCustomerOpen;
    [ObservableProperty] private string _quickCustomerName = string.Empty;
    [ObservableProperty] private string _quickCustomerPhone = string.Empty;

    // ─── Totals ──────────────────────────────────────────────────────────
    [ObservableProperty] private decimal _subTotal;
    [ObservableProperty] private decimal _totalDiscount;
    [ObservableProperty] private decimal _totalTax;
    [ObservableProperty] private decimal _grandTotal;
    [ObservableProperty] private int _totalItems;

    // ─── VAT Toggle ──────────────────────────────────────────────────────
    [ObservableProperty] private bool _isVatEnabled; // OFF by default

    // ─── Payment ─────────────────────────────────────────────────────────
    [ObservableProperty] private PaymentMode _paymentMode = PaymentMode.Cash;
    [ObservableProperty] private decimal _amountReceived;
    [ObservableProperty] private decimal _changeAmount;
    [ObservableProperty] private string _invoiceNumber = "Generating...";

    [ObservableProperty] private bool _isProcessing;

    // ─── Computed Booleans ────────────────────────────────────────────────
    [ObservableProperty] private bool _hasSearchResults;
    [ObservableProperty] private bool _isCashPayment = true;
    [ObservableProperty] private bool _hasCustomerResults;
    [ObservableProperty] private bool _canGenerateInvoice;

    // ─── Toast ───────────────────────────────────────────────────────────
    [ObservableProperty] private string _toastMessage = string.Empty;
    [ObservableProperty] private bool _isToastVisible;

    // ─── Post-Invoice Dialog ─────────────────────────────────────────────
    [ObservableProperty] private bool _isInvoiceSuccessOpen;
    [ObservableProperty] private string _lastInvoicePath = string.Empty;
    [ObservableProperty] private string _lastInvoiceNumber = string.Empty;
    [ObservableProperty] private string _lastCustomerPhone = string.Empty;
    [ObservableProperty] private decimal _lastGrandTotal;

    public BillingViewModel(
        IProductRepository productRepo,
        IInvoiceRepository invoiceRepo,
        ICustomerRepository customerRepo,
        AppConfigService configService)
    {
        _productRepo = productRepo;
        _invoiceRepo = invoiceRepo;
        _customerRepo = customerRepo;
        _configService = configService;

        CartItems.CollectionChanged += (s, e) => RecalculateTotals();
    }

    public override async Task LoadAsync()
    {
        InvoiceNumber = await _invoiceRepo.GenerateNextInvoiceNumberAsync();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // PRODUCT SEARCH (auto-suggest on every keystroke)
    // ═══════════════════════════════════════════════════════════════════════

    [RelayCommand]
    private async Task SearchProducts(string query)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 1)
        {
            SearchResults.Clear();
            HasSearchResults = false;
            return;
        }

        var results = await _productRepo.SearchAsync(query);
        SearchResults = new ObservableCollection<Product>(results.Where(p => p.Stock > 0));
        HasSearchResults = SearchResults.Count > 0;
    }

    [RelayCommand]
    private void AddToCart(Product product)
    {
        if (product == null) return;
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
                Discount = 0,
                TaxRate = IsVatEnabled ? 13.0m : 0m // Respect VAT toggle
            };
            newItem.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(CartItem.LineTotal))
                    RecalculateTotals();
            };
            CartItems.Add(newItem);
        }

        SearchQuery = string.Empty;
        SearchResults.Clear();
        HasSearchResults = false;
        ErrorMessage = null;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // VAT TOGGLE
    // ═══════════════════════════════════════════════════════════════════════

    partial void OnIsVatEnabledChanged(bool value)
    {
        // Update all existing cart items when VAT is toggled
        foreach (var item in CartItems)
        {
            item.TaxRate = value ? 13.0m : 0m;
        }
        RecalculateTotals();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // CART MANAGEMENT
    // ═══════════════════════════════════════════════════════════════════════

    [RelayCommand]
    private void IncrementQuantity(CartItem item)
    {
        if (item == null) return;
        if (item.Quantity < item.MaxStock)
        {
            item.Quantity++;
            RecalculateTotals();
        }
        else
        {
            ErrorMessage = $"Max stock reached: {item.MaxStock}";
        }
    }

    [RelayCommand]
    private void DecrementQuantity(CartItem item)
    {
        if (item == null) return;
        if (item.Quantity > 1)
        {
            item.Quantity--;
            RecalculateTotals();
        }
        else
        {
            CartItems.Remove(item);
        }
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
                ErrorMessage = null;
            }
        }
    }

    [RelayCommand]
    private void FocusSearch()
    {
        // Placeholder — keyboard shortcut F2 triggers this.
    }

    // ═══════════════════════════════════════════════════════════════════════
    // CUSTOMER (Required for invoicing)
    // ═══════════════════════════════════════════════════════════════════════

    [RelayCommand]
    private async Task SearchCustomers(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            CustomerResults.Clear();
            HasCustomerResults = false;
            return;
        }

        var results = await _customerRepo.SearchAsync(query);
        CustomerResults = new ObservableCollection<Customer>(results);
        HasCustomerResults = CustomerResults.Count > 0;
    }

    [RelayCommand]
    private void SelectCustomer(Customer? customer)
    {
        SelectedCustomer = customer;
        CustomerResults.Clear();
        CustomerSearchQuery = string.Empty;
        HasCustomerResults = false;
        ErrorMessage = null;
    }

    [RelayCommand]
    private void OpenQuickAddCustomer()
    {
        QuickCustomerName = string.Empty;
        QuickCustomerPhone = string.Empty;
        IsQuickAddCustomerOpen = true;
    }

    [RelayCommand]
    private void CloseQuickAddCustomer()
    {
        IsQuickAddCustomerOpen = false;
    }

    [RelayCommand]
    private async Task SaveQuickCustomer()
    {
        if (string.IsNullOrWhiteSpace(QuickCustomerName))
        {
            ErrorMessage = "Customer name is required.";
            return;
        }
        if (string.IsNullOrWhiteSpace(QuickCustomerPhone))
        {
            ErrorMessage = "Phone number is required for invoicing.";
            return;
        }

        try
        {
            var customer = new Customer
            {
                Name = QuickCustomerName,
                Phone = QuickCustomerPhone
            };
            customer.Id = await _customerRepo.AddAsync(customer);
            SelectedCustomer = customer;
            IsQuickAddCustomerOpen = false;
            ErrorMessage = null;
            ShowToast($"Customer '{customer.Name}' added!");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to quick-add customer");
            ErrorMessage = $"Failed to add customer: {ex.Message}";
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // PAYMENT & INVOICE
    // ═══════════════════════════════════════════════════════════════════════

    [RelayCommand]
    private void SetPaymentMode(string mode)
    {
        PaymentMode = mode switch
        {
            "Cash" => PaymentMode.Cash,
            "Card" => PaymentMode.Card,
            "UPI" => PaymentMode.UPI,
            "Credit" => PaymentMode.Credit,
            _ => PaymentMode.Cash
        };
        IsCashPayment = PaymentMode == PaymentMode.Cash;
        CalculateChange();
    }

    [RelayCommand]
    private async Task GenerateInvoice()
    {
        // Validation
        if (CartItems.Count == 0)
        {
            ErrorMessage = "Cart is empty! Add products first.";
            return;
        }

        if (SelectedCustomer == null)
        {
            // Customer is optional — allow walk-in
        }

        if (IsCashPayment && AmountReceived < GrandTotal)
        {
            ErrorMessage = $"Amount received (NPR {AmountReceived:N2}) is less than total (NPR {GrandTotal:N2}).";
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
                CustomerName = SelectedCustomer?.Name ?? "Walk-in Customer",
                Date = DateTime.UtcNow,
                TotalAmount = GrandTotal,
                TaxAmount = TotalTax,
                DiscountAmount = TotalDiscount,
                PaymentStatus = PaymentMode == PaymentMode.Credit ? PaymentStatus.Unpaid : PaymentStatus.Paid,
                PaymentMode = PaymentMode,
                Items = CartItems.Select(c => new InvoiceItem
                {
                    ProductId = c.ProductId,
                    ProductName = c.ProductName,
                    Quantity = c.Quantity,
                    Price = c.UnitPrice,
                    Discount = (c.UnitPrice * c.Quantity) * (c.Discount / 100m)
                }).ToList()
            };

            var invoiceId = await _invoiceRepo.AddAsync(invoice);

            // Generate PDF Invoice
            var outputDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "NepalKhata", "Invoices");
            var businessProfile = _configService.BusinessProfile ?? new BusinessProfile { Name = "NepalKhata" };

            var pdfPath = await Task.Run(() =>
                InvoicePdfService.GeneratePdf(invoice, businessProfile, outputDir));

            // Store for post-invoice dialog
            LastInvoicePath = pdfPath;
            LastInvoiceNumber = invoice.InvoiceNumber;
            LastCustomerPhone = SelectedCustomer?.Phone ?? string.Empty;
            LastGrandTotal = GrandTotal;

            Logger.Information("Invoice {InvoiceNumber} generated. Total: {Total}. PDF: {Path}",
                invoice.InvoiceNumber, GrandTotal, pdfPath);

            // Reset cart
            CartItems.Clear();
            SelectedCustomer = null;
            AmountReceived = 0;
            InvoiceNumber = await _invoiceRepo.GenerateNextInvoiceNumberAsync();
            ErrorMessage = null;

            // Show success dialog with options
            IsInvoiceSuccessOpen = true;
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

    // ═══════════════════════════════════════════════════════════════════════
    // POST-INVOICE ACTIONS
    // ═══════════════════════════════════════════════════════════════════════

    [RelayCommand]
    private void OpenInvoicePdf()
    {
        if (!string.IsNullOrEmpty(LastInvoicePath) && File.Exists(LastInvoicePath))
        {
            Process.Start(new ProcessStartInfo(LastInvoicePath) { UseShellExecute = true });
        }
    }

    [RelayCommand]
    private void PrintInvoice()
    {
        if (!string.IsNullOrEmpty(LastInvoicePath) && File.Exists(LastInvoicePath))
        {
            try
            {
                Process.Start(new ProcessStartInfo(LastInvoicePath)
                {
                    UseShellExecute = true,
                    Verb = "print"
                });
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to print invoice");
                // Fallback: just open it
                Process.Start(new ProcessStartInfo(LastInvoicePath) { UseShellExecute = true });
            }
        }
    }

    [RelayCommand]
    private void ShareWhatsApp()
    {
        if (string.IsNullOrEmpty(LastCustomerPhone)) return;

        // Clean phone number (remove spaces, dashes)
        var phone = LastCustomerPhone.Replace(" ", "").Replace("-", "");
        // Add Nepal country code if not present
        if (!phone.StartsWith("+"))
        {
            phone = phone.StartsWith("977") ? $"+{phone}" : $"+977{phone}";
        }

        var message = Uri.EscapeDataString(
            $"Dear Customer,\n\n" +
            $"Your invoice *{LastInvoiceNumber}* has been generated.\n" +
            $"Total Amount: *NPR {LastGrandTotal:N2}*\n\n" +
            $"Thank you for your purchase! 🙏");

        var whatsappUrl = $"https://wa.me/{phone.TrimStart('+')}?text={message}";

        try
        {
            Process.Start(new ProcessStartInfo(whatsappUrl) { UseShellExecute = true });
            ShowToast("Opening WhatsApp...");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to open WhatsApp");
            ErrorMessage = "Could not open WhatsApp. Please check your browser.";
        }
    }

    [RelayCommand]
    private void CloseInvoiceSuccess()
    {
        IsInvoiceSuccessOpen = false;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // HELPERS
    // ═══════════════════════════════════════════════════════════════════════

    private void RecalculateTotals()
    {
        SubTotal = CartItems.Sum(i => i.UnitPrice * i.Quantity);
        TotalDiscount = CartItems.Sum(i => (i.UnitPrice * i.Quantity) * (i.Discount / 100m));
        TotalTax = IsVatEnabled
            ? CartItems.Sum(i => i.LineTotal * (13.0m / 100m))
            : 0m;
        GrandTotal = (SubTotal - TotalDiscount) + TotalTax;
        TotalItems = CartItems.Sum(i => i.Quantity);
        CanGenerateInvoice = CartItems.Count > 0;
        CalculateChange();
    }

    partial void OnAmountReceivedChanged(decimal value)
    {
        CalculateChange();
    }

    private void CalculateChange()
    {
        ChangeAmount = IsCashPayment ? AmountReceived - GrandTotal : 0;
    }

    private async void ShowToast(string message)
    {
        ToastMessage = message;
        IsToastVisible = true;
        await Task.Delay(3000);
        IsToastVisible = false;
    }

    /// <summary>
    /// Public method called from code-behind to clear search results when search box is empty.
    /// </summary>
    public void ClearSearchResults()
    {
        SearchResults.Clear();
        HasSearchResults = false;
    }
}
