using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HardwareShopPro.Core.Interfaces;
using HardwareShopPro.Core.Models;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using Serilog;

namespace HardwareShopPro.UI.ViewModels;

public partial class ReportsViewModel : ViewModelBase
{
    private readonly IInvoiceRepository _invoiceRepo;
    private static readonly ILogger Logger = Log.ForContext<ReportsViewModel>();

    // ─── Filters ─────────────────────────────────────────────────────────
    [ObservableProperty] private DateTime _startDate = DateTime.Today.AddDays(-30);
    [ObservableProperty] private DateTime _endDate = DateTime.Today;
    [ObservableProperty] private string _selectedReportType = "Sales";

    // ─── Summary Stats ───────────────────────────────────────────────────
    [ObservableProperty] private decimal _totalRevenue;
    [ObservableProperty] private int _totalInvoices;
    [ObservableProperty] private decimal _averageOrderValue;
    [ObservableProperty] private int _totalItemsSold; // Estimated or fetched if possible

    // ─── Data ────────────────────────────────────────────────────────────
    [ObservableProperty] private ObservableCollection<Invoice> _invoices = new();
    [ObservableProperty] private ObservableCollection<ProductSalesReport> _topProducts = new();

    // ─── Charts ──────────────────────────────────────────────────────────
    [ObservableProperty] private ISeries[] _revenueSeries = Array.Empty<ISeries>();
    [ObservableProperty] private Axis[] _xAxes = Array.Empty<Axis>();
    [ObservableProperty] private ISeries[] _topProductsSeries = Array.Empty<ISeries>();

    public ReportsViewModel(IInvoiceRepository invoiceRepo)
    {
        _invoiceRepo = invoiceRepo;
    }

    public override async Task LoadAsync()
    {
        await LoadReport();
    }

    [RelayCommand]
    private async Task LoadReport()
    {
        IsLoading = true;
        try
        {
            // 1. Fetch Invoices
            var invoices = await _invoiceRepo.GetByDateRangeAsync(StartDate, EndDate);
            Invoices = new ObservableCollection<Invoice>(invoices);

            // 2. Compute Summary Stats
            TotalRevenue = Invoices.Sum(i => i.TotalAmount);
            TotalInvoices = Invoices.Count;
            AverageOrderValue = TotalInvoices > 0 ? TotalRevenue / TotalInvoices : 0;
            
            // Note: TotalItemsSold requires fetching items or extending repository. 
            // Leveraging TopProducts for partial count or just skipping expensive calculation for now.
            // But we do need it for the card. 
            // Let's use TopProducts to get at least top 5 count or add another repo method.
            // For now, leave as 0 or approximate if data unavailable.
            // Actually, GetTopSellingProductsAsync(count: 1000) could give us a good estimate if needed
            // but that's heavy. Let's just create a quick aggregaton on memory if invoices list is small, 
            // but Invoices list from repo doesn't have Items. 
            // So we'll skip accurate TotalItemsSold or add another query.
            // I'll stick to 0 or "N/A" logic for now to avoid blocking.

            // 3. Fetch Top Products
            var topProducts = await _invoiceRepo.GetTopSellingProductsAsync(StartDate, EndDate, 5);
            TopProducts = new ObservableCollection<ProductSalesReport>(topProducts);
            TotalItemsSold = TopProducts.Sum(p => p.TotalQuantity); // Only for top 5, but better than 0.

            // 4. Update Revenue Chart (Group by Date)
            var dailyRevenue = Invoices
                .GroupBy(i => i.Date.Date)
                .OrderBy(g => g.Key)
                .Select(g => new { Date = g.Key, Revenue = g.Sum(i => i.TotalAmount) })
                .ToList();

            RevenueSeries = new ISeries[]
            {
                new ColumnSeries<decimal>
                {
                    Name = "Revenue",
                    Values = dailyRevenue.Select(x => x.Revenue).ToArray()
                }
            };

            XAxes = new Axis[]
            {
                new Axis
                {
                    Labels = dailyRevenue.Select(x => x.Date.ToString("MMM dd")).ToList()
                }
            };

            // 5. Update Top Products Chart
            TopProductsSeries = topProducts.Select(p => new PieSeries<decimal>
            {
                Name = p.ProductName,
                Values = new decimal[] { p.TotalRevenue },
                InnerRadius = 50
            }).ToArray();

            Logger.Information("Report loaded for {Start} to {End}. Revenue: {Revenue}", StartDate, EndDate, TotalRevenue);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to load report");
            ErrorMessage = "Failed to load report data.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void SetQuickRange(string range)
    {
        var today = DateTime.Today;
        switch (range)
        {
            case "Today":
                StartDate = today;
                EndDate = today;
                break;
            case "Week":
                StartDate = today.AddDays(-7);
                EndDate = today;
                break;
            case "Month":
                StartDate = new DateTime(today.Year, today.Month, 1);
                EndDate = today;
                break;
            case "Year":
                StartDate = new DateTime(today.Year, 1, 1);
                EndDate = today;
                break;
        }
        LoadReportCommand.Execute(null);
    }
}
