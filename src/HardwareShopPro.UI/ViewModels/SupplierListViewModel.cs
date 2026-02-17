using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HardwareShopPro.Core.Interfaces;
using HardwareShopPro.Core.Models;

namespace HardwareShopPro.UI.ViewModels;

public partial class SupplierListViewModel : ViewModelBase
{
    private readonly ISupplierRepository _supplierRepo;

    [ObservableProperty] private ObservableCollection<Supplier> _suppliers = new();
    [ObservableProperty] private Supplier? _selectedSupplier;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private bool _isDialogOpen;
    [ObservableProperty] private bool _isEditMode;
    [ObservableProperty] private Supplier _editingSupplier = new();

    public SupplierListViewModel(ISupplierRepository supplierRepo) => _supplierRepo = supplierRepo;

    public override async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var items = await _supplierRepo.GetAllAsync();
            Suppliers = new ObservableCollection<Supplier>(items);
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task Search()
    {
        var results = string.IsNullOrWhiteSpace(SearchText)
            ? await _supplierRepo.GetAllAsync()
            : await _supplierRepo.SearchAsync(SearchText);
        Suppliers = new ObservableCollection<Supplier>(results);
    }

    [RelayCommand]
    private void OpenAddDialog() { IsEditMode = false; EditingSupplier = new Supplier(); IsDialogOpen = true; }

    [RelayCommand]
    private void OpenEditDialog()
    {
        if (SelectedSupplier == null) return;
        IsEditMode = true;
        EditingSupplier = new Supplier
        {
            Id = SelectedSupplier.Id, Name = SelectedSupplier.Name,
            Contact = SelectedSupplier.Contact, Email = SelectedSupplier.Email,
            Address = SelectedSupplier.Address, GSTIN = SelectedSupplier.GSTIN
        };
        IsDialogOpen = true;
    }

    [RelayCommand]
    private async Task Save()
    {
        if (string.IsNullOrWhiteSpace(EditingSupplier.Name)) { ErrorMessage = "Name is required."; return; }
        if (IsEditMode) await _supplierRepo.UpdateAsync(EditingSupplier);
        else await _supplierRepo.AddAsync(EditingSupplier);
        IsDialogOpen = false;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task Delete()
    {
        if (SelectedSupplier == null) return;
        await _supplierRepo.DeleteAsync(SelectedSupplier.Id);
        Suppliers.Remove(SelectedSupplier);
    }

    [RelayCommand] private void CancelDialog() { IsDialogOpen = false; ErrorMessage = null; }
}
