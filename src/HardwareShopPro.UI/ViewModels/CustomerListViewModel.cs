using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HardwareShopPro.Core.Interfaces;
using HardwareShopPro.Core.Models;

namespace HardwareShopPro.UI.ViewModels;

public partial class CustomerListViewModel : ViewModelBase
{
    private readonly ICustomerRepository _customerRepo;

    [ObservableProperty] private ObservableCollection<Customer> _customers = new();
    [ObservableProperty] private Customer? _selectedCustomer;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private bool _isDialogOpen;
    [ObservableProperty] private bool _isEditMode;
    [ObservableProperty] private Customer _editingCustomer = new();

    public CustomerListViewModel(ICustomerRepository customerRepo) => _customerRepo = customerRepo;

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

    [RelayCommand]
    private void OpenAddDialog() { IsEditMode = false; EditingCustomer = new Customer(); IsDialogOpen = true; }

    [RelayCommand]
    private void OpenEditDialog()
    {
        if (SelectedCustomer == null) return;
        IsEditMode = true;
        EditingCustomer = new Customer
        {
            Id = SelectedCustomer.Id, Name = SelectedCustomer.Name,
            Phone = SelectedCustomer.Phone, Email = SelectedCustomer.Email,
            Address = SelectedCustomer.Address, GSTIN = SelectedCustomer.GSTIN
        };
        IsDialogOpen = true;
    }

    [RelayCommand]
    private async Task Save()
    {
        if (string.IsNullOrWhiteSpace(EditingCustomer.Name)) { ErrorMessage = "Name is required."; return; }
        if (IsEditMode) await _customerRepo.UpdateAsync(EditingCustomer);
        else await _customerRepo.AddAsync(EditingCustomer);
        IsDialogOpen = false;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task Delete()
    {
        if (SelectedCustomer == null) return;
        await _customerRepo.DeleteAsync(SelectedCustomer.Id);
        Customers.Remove(SelectedCustomer);
    }

    [RelayCommand] private void CancelDialog() { IsDialogOpen = false; ErrorMessage = null; }
}
