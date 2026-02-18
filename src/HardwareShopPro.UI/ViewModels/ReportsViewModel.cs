using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HardwareShopPro.Core.Interfaces;
using HardwareShopPro.Core.Models;
using HardwareShopPro.Core.Services;
using HardwareShopPro.UI.Services;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using Serilog;

namespace HardwareShopPro.UI.ViewModels;

public partial class ReportsViewModel : ViewModelBase
{
    private readonly IInvoiceRepository _invoiceRepo;
    private readonly AppConfigService _configService;
    private static readonly ILogger Logger = Log.ForContext<ReportsViewModel>();

    // ─── Filters ─────────────────────────────────────────────────────────
    [ObservableProperty] private DateTime _startDate = DateTime.Today.AddDays(-30);
    [ObservableProperty] private DateTime _endDate = DateTime.Today;
    [ObservableProperty] private string _selectedReportType = "Sales";

    // ─── Summary Stats ───────────────────────────────────────────────────
    [ObservableProperty] private decimal _totalRevenue;
    [ObservableProperty] private int _totalInvoices;
    [ObservableProperty] private decimal _averageOrderValue;
    [ObservableProperty] private int _totalItemsSold;

    // ─── Data ────────────────────────────────────────────────────────────
    [ObservableProperty] private ObservableCollection<Invoice> _invoices = new();
    [ObservableProperty] private ObservableCollection<ProductSalesReport> _topProducts = new();

    // ─── Charts ──────────────────────────────────────────────────────────
    [ObservableProperty] private ISeries[] _revenueSeries = Array.Empty<ISeries>();
    [ObservableProperty] private Axis[] _xAxes = Array.Empty<Axis>();
    [ObservableProperty] private ISeries[] _topProductsSeries = Array.Empty<ISeries>();

    // ─── Export ──────────────────────────────────────────────────────────
    [ObservableProperty] private bool _isExporting;
    [ObservableProperty] private string _toastMessage = string.Empty;
    [ObservableProperty] private bool _isToastVisible;

    public ReportsViewModel(IInvoiceRepository invoiceRepo, AppConfigService configService)
    {
        _invoiceRepo = invoiceRepo;
        _configService = configService;
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
            var invoices = await _invoiceRepo.GetByDateRangeAsync(StartDate, EndDate);
            Invoices = new ObservableCollection<Invoice>(invoices);

            TotalRevenue = Invoices.Sum(i => i.TotalAmount);
            TotalInvoices = Invoices.Count;
            AverageOrderValue = TotalInvoices > 0 ? TotalRevenue / TotalInvoices : 0;

            var topProducts = await _invoiceRepo.GetTopSellingProductsAsync(StartDate, EndDate, 5);
            TopProducts = new ObservableCollection<ProductSalesReport>(topProducts);
            TotalItemsSold = TopProducts.Sum(p => p.TotalQuantity);

            // Revenue Chart (daily)
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

            // Top Products pie chart
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

    // ═══════════════════════════════════════════════════════════════════════
    // EXPORT
    // ═══════════════════════════════════════════════════════════════════════

    [RelayCommand]
    private async Task ExportPdf()
    {
        if (Invoices.Count == 0) { ErrorMessage = "No data to export. Generate a report first."; return; }

        IsExporting = true;
        try
        {
            var outputDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "NepalKhata", "Reports");
            var businessName = _configService.BusinessProfile?.Name ?? "NepalKhata";

            var filePath = await Task.Run(() => ReportExportService.ExportSalesReportPdf(
                SelectedReportType, StartDate, EndDate,
                TotalRevenue, TotalInvoices, AverageOrderValue,
                Invoices, TopProducts, businessName, outputDir));

            ShowToast($"PDF exported successfully!");

            // Open file
            Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "PDF export failed");
            ErrorMessage = $"Export failed: {ex.Message}";
        }
        finally
        {
            IsExporting = false;
        }
    }

    [RelayCommand]
    private async Task ExportExcel()
    {
        if (Invoices.Count == 0) { ErrorMessage = "No data to export. Generate a report first."; return; }

        IsExporting = true;
        try
        {
            var outputDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "NepalKhata", "Reports");
            var businessName = _configService.BusinessProfile?.Name ?? "NepalKhata";

            var filePath = await Task.Run(() => ReportExportService.ExportSalesReportExcel(
                SelectedReportType, StartDate, EndDate,
                TotalRevenue, TotalInvoices, AverageOrderValue,
                Invoices, TopProducts, businessName, outputDir));

            ShowToast($"Excel exported successfully!");

            // Open file
            Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Excel export failed");
            ErrorMessage = $"Export failed: {ex.Message}";
        }
        finally
        {
            IsExporting = false;
        }
    }

    private async void ShowToast(string message)
    {
        ToastMessage = message;
        IsToastVisible = true;
        await Task.Delay(3000);
        IsToastVisible = false;
    }
}
