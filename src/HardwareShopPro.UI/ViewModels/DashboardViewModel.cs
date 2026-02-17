using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HardwareShopPro.Core.Interfaces;
using HardwareShopPro.Core.Models;
using Serilog;

namespace HardwareShopPro.UI.ViewModels;

/// <summary>
/// Dashboard ViewModel displaying real-time stats, low stock alerts, and sales overview.
/// </summary>
public partial class DashboardViewModel : ViewModelBase
{
    private readonly IInvoiceRepository _invoiceRepo;
    private readonly IProductRepository _productRepo;
    private static readonly ILogger Logger = Log.ForContext<DashboardViewModel>();

    [ObservableProperty] private int _totalProducts;
    [ObservableProperty] private int _lowStockCount;
    [ObservableProperty] private int _todaySalesCount;
    [ObservableProperty] private decimal _todayRevenue;
    [ObservableProperty] private decimal _monthlyRevenue;
    [ObservableProperty] private int _totalCustomers;
    [ObservableProperty] private int _totalSuppliers;
    [ObservableProperty] private List<Product> _lowStockProducts = new();
    [ObservableProperty] private List<DailySales> _last7DaysSales = new();

    // Chart data properties
    [ObservableProperty] private double[] _chartValues = Array.Empty<double>();
    [ObservableProperty] private string[] _chartLabels = Array.Empty<string>();

    public DashboardViewModel(IInvoiceRepository invoiceRepo, IProductRepository productRepo)
    {
        _invoiceRepo = invoiceRepo;
        _productRepo = productRepo;
    }

    public override async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var stats = await _invoiceRepo.GetDashboardStatsAsync();
            TotalProducts = stats.TotalProducts;
            LowStockCount = stats.LowStockCount;
            TodaySalesCount = stats.TodaySalesCount;
            TodayRevenue = stats.TodayRevenue;
            MonthlyRevenue = stats.MonthlyRevenue;
            TotalCustomers = stats.TotalCustomers;
            TotalSuppliers = stats.TotalSuppliers;
            LowStockProducts = stats.LowStockProducts;
            Last7DaysSales = stats.Last7DaysSales;

            // Prepare chart data
            ChartValues = stats.Last7DaysSales.Select(s => (double)s.Revenue).ToArray();
            ChartLabels = stats.Last7DaysSales.Select(s => s.Date.ToString("MMM dd")).ToArray();

            Logger.Information("Dashboard loaded: {Products} products, {Sales} today's sales",
                TotalProducts, TodaySalesCount);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to load dashboard");
            ErrorMessage = "Failed to load dashboard data.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Refresh()
    {
        await LoadAsync();
    }
}
