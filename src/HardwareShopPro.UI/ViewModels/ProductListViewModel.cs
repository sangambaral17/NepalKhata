using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HardwareShopPro.Core.Interfaces;
using HardwareShopPro.Core.Models;
using HardwareShopPro.Core.Services;
using Serilog;

namespace HardwareShopPro.UI.ViewModels;

/// <summary>
/// Product list ViewModel with full CRUD, search (standard + AI), and filtering.
/// </summary>
public partial class ProductListViewModel : ViewModelBase
{
    private readonly IProductRepository _productRepo;
    private readonly ISupplierRepository _supplierRepo;
    private readonly IAIService _aiService;
    private readonly AuthenticationService _authService;
    private readonly IAuditLogRepository _auditRepo;
    private static readonly ILogger Logger = Log.ForContext<ProductListViewModel>();

    [ObservableProperty] private ObservableCollection<Product> _products = new();
    [ObservableProperty] private Product? _selectedProduct;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private bool _isAISearchEnabled;
    [ObservableProperty] private string? _aiSearchStatus;
    [ObservableProperty] private ObservableCollection<string> _categories = new();
    [ObservableProperty] private ObservableCollection<string> _brands = new();
    [ObservableProperty] private string? _selectedCategory;
    [ObservableProperty] private string? _selectedBrand;

    // Dialog state
    [ObservableProperty] private bool _isDialogOpen;
    [ObservableProperty] private bool _isEditMode;
    [ObservableProperty] private Product _editingProduct = new();
    [ObservableProperty] private ObservableCollection<Supplier> _suppliers = new();

    public ProductListViewModel(
        IProductRepository productRepo,
        ISupplierRepository supplierRepo,
        IAIService aiService,
        AuthenticationService authService,
        IAuditLogRepository auditRepo)
    {
        _productRepo = productRepo;
        _supplierRepo = supplierRepo;
        _aiService = aiService;
        _authService = authService;
        _auditRepo = auditRepo;
    }

    public override async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var products = await _productRepo.GetAllAsync();
            Products = new ObservableCollection<Product>(products);

            var cats = await _productRepo.GetCategoriesAsync();
            Categories = new ObservableCollection<string>(cats);

            var brands = await _productRepo.GetBrandsAsync();
            Brands = new ObservableCollection<string>(brands);

            var suppliers = await _supplierRepo.GetAllAsync();
            Suppliers = new ObservableCollection<Supplier>(suppliers);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to load products");
            ErrorMessage = "Failed to load products.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Search()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            await LoadAsync();
            return;
        }

        IsLoading = true;
        AiSearchStatus = null;

        try
        {
            IEnumerable<Product> results;

            if (IsAISearchEnabled)
            {
                AiSearchStatus = "🤖 AI analyzing query...";
                var criteria = await _aiService.SmartSearchAsync(SearchText);
                if (criteria != null)
                {
                    results = await _productRepo.SearchByCriteriaAsync(criteria);
                    AiSearchStatus = $"🤖 AI found {results.Count()} results";
                }
                else
                {
                    // Fallback to standard search
                    results = await _productRepo.SearchAsync(SearchText);
                    AiSearchStatus = "⚠️ AI unavailable — used standard search";
                }
            }
            else
            {
                results = await _productRepo.SearchAsync(SearchText);
            }

            Products = new ObservableCollection<Product>(results);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Search failed");
            ErrorMessage = "Search failed.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task FilterByCategory()
    {
        if (string.IsNullOrEmpty(SelectedCategory))
        {
            await LoadAsync();
            return;
        }

        IsLoading = true;
        try
        {
            var results = await _productRepo.GetByCategoryAsync(SelectedCategory);
            Products = new ObservableCollection<Product>(results);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void OpenAddDialog()
    {
        IsEditMode = false;
        EditingProduct = new Product { MinStockLevel = 5 };
        IsDialogOpen = true;
    }

    [RelayCommand]
    private void OpenEditDialog()
    {
        if (SelectedProduct == null) return;
        IsEditMode = true;
        EditingProduct = new Product
        {
            Id = SelectedProduct.Id,
            Name = SelectedProduct.Name,
            Category = SelectedProduct.Category,
            Brand = SelectedProduct.Brand,
            SKU = SelectedProduct.SKU,
            PurchasePrice = SelectedProduct.PurchasePrice,
            SellingPrice = SelectedProduct.SellingPrice,
            Stock = SelectedProduct.Stock,
            MinStockLevel = SelectedProduct.MinStockLevel,
            SupplierId = SelectedProduct.SupplierId
        };
        IsDialogOpen = true;
    }

    [RelayCommand]
    private async Task SaveProduct()
    {
        if (string.IsNullOrWhiteSpace(EditingProduct.Name))
        {
            ErrorMessage = "Product name is required.";
            return;
        }

        try
        {
            if (IsEditMode)
            {
                await _productRepo.UpdateAsync(EditingProduct);
                await LogAuditAsync("UPDATE_PRODUCT", "Product", EditingProduct.Id,
                    $"Updated product: {EditingProduct.Name}");
            }
            else
            {
                var id = await _productRepo.AddAsync(EditingProduct);
                await LogAuditAsync("CREATE_PRODUCT", "Product", id,
                    $"Created product: {EditingProduct.Name}");
            }

            IsDialogOpen = false;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to save product");
            ErrorMessage = "Failed to save product. Check if SKU is unique.";
        }
    }

    [RelayCommand]
    private async Task DeleteProduct()
    {
        if (SelectedProduct == null) return;

        try
        {
            var success = await _productRepo.DeleteAsync(SelectedProduct.Id);
            if (success)
            {
                await LogAuditAsync("DELETE_PRODUCT", "Product", SelectedProduct.Id,
                    $"Deleted product: {SelectedProduct.Name}");
                Products.Remove(SelectedProduct);
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to delete product");
            ErrorMessage = "Cannot delete product — it may be linked to invoices.";
        }
    }

    [RelayCommand]
    private void CancelDialog()
    {
        IsDialogOpen = false;
        ErrorMessage = null;
    }

    [RelayCommand]
    private async Task ClearFilters()
    {
        SearchText = string.Empty;
        SelectedCategory = null;
        SelectedBrand = null;
        AiSearchStatus = null;
        await LoadAsync();
    }

    private async Task LogAuditAsync(string action, string entity, int entityId, string details)
    {
        if (_authService.CurrentUser == null) return;
        await _auditRepo.AddAsync(new AuditLog
        {
            UserId = _authService.CurrentUser.Id,
            Username = _authService.CurrentUser.Username,
            Action = action,
            Entity = entity,
            EntityId = entityId,
            Details = details
        });
    }
}
