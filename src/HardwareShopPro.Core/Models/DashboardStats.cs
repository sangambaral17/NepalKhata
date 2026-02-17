namespace HardwareShopPro.Core.Models;

/// <summary>
/// Data transfer object for dashboard statistics.
/// </summary>
public class DashboardStats
{
    public int TotalProducts { get; set; }
    public int LowStockCount { get; set; }
    public int TodaySalesCount { get; set; }
    public decimal TodayRevenue { get; set; }
    public decimal MonthlyRevenue { get; set; }
    public int TotalCustomers { get; set; }
    public int TotalSuppliers { get; set; }
    public List<DailySales> Last7DaysSales { get; set; } = new();
    public List<Product> LowStockProducts { get; set; } = new();
}

/// <summary>
/// Sales data for a single day, used in charts.
/// </summary>
public class DailySales
{
    public DateTime Date { get; set; }
    public decimal Revenue { get; set; }
    public int OrderCount { get; set; }
}
