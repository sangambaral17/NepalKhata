using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HardwareShopPro.Core.Interfaces;
using HardwareShopPro.Core.Models;
using Serilog;

namespace HardwareShopPro.UI.ViewModels;

public partial class CustomerListViewModel : ViewModelBase
{
    private readonly ICustomerRepository _customerRepo;
    private readonly IInvoiceRepository _invoiceRepo;
    private static readonly ILogger Logger = Log.ForContext<CustomerListViewModel>();

    [ObservableProperty] private ObservableCollection<Customer> _customers = new();
    [ObservableProperty] private Customer? _selectedCustomer;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private bool _isDialogOpen;
    [ObservableProperty] private bool _isEditMode;
    [ObservableProperty] private Customer _editingCustomer = new();

    // ─── Detail Panel ────────────────────────────────────────────────────
    [ObservableProperty] private bool _isDetailOpen;
    [ObservableProperty] private ObservableCollection<Invoice> _customerInvoices = new();
    [ObservableProperty] private decimal _customerTotalSpent;
    [ObservableProperty] private int _customerInvoiceCount;

    // ─── Toast ───────────────────────────────────────────────────────────
    [ObservableProperty] private string _toastMessage = string.Empty;
    [ObservableProperty] private bool _isToastVisible;

    public CustomerListViewModel(ICustomerRepository customerRepo, IInvoiceRepository invoiceRepo)
    {
        _customerRepo = customerRepo;
        _invoiceRepo = invoiceRepo;
    }

    public override async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var items = await _customerRepo.GetAllAsync();
            Customers = new ObservableCollection<Customer>(items);
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task Search()
    {
        var results = string.IsNullOrWhiteSpace(SearchText)
            ? await _customerRepo.GetAllAsync()
            : await _customerRepo.SearchAsync(SearchText);
        Customers = new ObservableCollection<Customer>(results);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ADD / EDIT / DELETE
    // ═══════════════════════════════════════════════════════════════════════

    [RelayCommand]
    private void OpenAddDialog()
    {
        IsEditMode = false;
        EditingCustomer = new Customer();
        IsDialogOpen = true;
        ErrorMessage = null;
    }

    [RelayCommand]
    private void OpenEditDialog()
    {
        if (SelectedCustomer == null) return;
        IsEditMode = true;
        EditingCustomer = new Customer
        {
            Id = SelectedCustomer.Id,
            Name = SelectedCustomer.Name,
            Phone = SelectedCustomer.Phone,
            Email = SelectedCustomer.Email,
            Address = SelectedCustomer.Address,
            GSTIN = SelectedCustomer.GSTIN
        };
        IsDialogOpen = true;
        ErrorMessage = null;
    }

    [RelayCommand]
    private async Task Save()
    {
        // Validation
        if (string.IsNullOrWhiteSpace(EditingCustomer.Name))
        {
            ErrorMessage = "Customer name is required.";
            return;
        }

        if (!string.IsNullOrWhiteSpace(EditingCustomer.Email) &&
            !Regex.IsMatch(EditingCustomer.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
        {
            ErrorMessage = "Please enter a valid email address.";
            return;
        }

        if (!string.IsNullOrWhiteSpace(EditingCustomer.Phone) &&
            !Regex.IsMatch(EditingCustomer.Phone, @"^\d{7,15}$"))
        {
            ErrorMessage = "Phone number should be 7-15 digits.";
            return;
        }

        try
        {
            if (IsEditMode)
            {
                await _customerRepo.UpdateAsync(EditingCustomer);
                ShowToast($"Customer '{EditingCustomer.Name}' updated.");
            }
            else
            {
                await _customerRepo.AddAsync(EditingCustomer);
                ShowToast($"Customer '{EditingCustomer.Name}' created.");
            }

            IsDialogOpen = false;
            ErrorMessage = null;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to save customer");
            ErrorMessage = $"Save failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task Delete()
    {
        if (SelectedCustomer == null) return;

        try
        {
            await _customerRepo.DeleteAsync(SelectedCustomer.Id);
            ShowToast($"Customer '{SelectedCustomer.Name}' deleted.");
            Customers.Remove(SelectedCustomer);
            SelectedCustomer = null;
            IsDetailOpen = false;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to delete customer");
            ErrorMessage = $"Delete failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private void CancelDialog()
    {
        IsDialogOpen = false;
        ErrorMessage = null;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // DETAIL PANEL
    // ═══════════════════════════════════════════════════════════════════════

    [RelayCommand]
    private async Task ViewDetails(Customer? customer)
    {
        if (customer == null) return;
        SelectedCustomer = customer;

        try
        {
            var invoices = await _invoiceRepo.GetByCustomerAsync(customer.Id);
            CustomerInvoices = new ObservableCollection<Invoice>(invoices);
            CustomerTotalSpent = CustomerInvoices.Sum(i => i.TotalAmount);
            CustomerInvoiceCount = CustomerInvoices.Count;
            IsDetailOpen = true;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to load customer details");
            ErrorMessage = $"Failed to load details: {ex.Message}";
        }
    }

    [RelayCommand]
    private void CloseDetail()
    {
        IsDetailOpen = false;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TOAST
    // ═══════════════════════════════════════════════════════════════════════

    private async void ShowToast(string message)
    {
        ToastMessage = message;
        IsToastVisible = true;
        await Task.Delay(3000);
        IsToastVisible = false;
    }
}
