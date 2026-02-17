using Dapper;
using HardwareShopPro.Core.Interfaces;
using HardwareShopPro.Core.Models;
using HardwareShopPro.Data.Database;
using Serilog;

namespace HardwareShopPro.Data.Repositories;

public class InvoiceRepository : IInvoiceRepository
{
    private readonly DatabaseContext _db;
    private static readonly ILogger Logger = Log.ForContext<InvoiceRepository>();

    public InvoiceRepository(DatabaseContext db) => _db = db;

    public async Task<IEnumerable<Invoice>> GetAllAsync()
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<Invoice>(@"
            SELECT i.*, c.Name AS CustomerName
            FROM Invoices i
            LEFT JOIN Customers c ON i.CustomerId = c.Id
            ORDER BY i.Date DESC");
    }

    public async Task<Invoice?> GetByIdAsync(int id)
    {
        using var conn = _db.CreateConnection();
        var invoice = await conn.QueryFirstOrDefaultAsync<Invoice>(@"
            SELECT i.*, c.Name AS CustomerName
            FROM Invoices i
            LEFT JOIN Customers c ON i.CustomerId = c.Id
            WHERE i.Id = @Id", new { Id = id });

        if (invoice != null)
        {
            invoice.Items = (await conn.QueryAsync<InvoiceItem>(@"
                SELECT ii.*, p.Name AS ProductName
                FROM InvoiceItems ii
                LEFT JOIN Products p ON ii.ProductId = p.Id
                WHERE ii.InvoiceId = @InvoiceId", new { InvoiceId = id })).ToList();
        }
        return invoice;
    }

    public async Task<IEnumerable<Invoice>> GetByDateRangeAsync(DateTime from, DateTime to)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<Invoice>(@"
            SELECT i.*, c.Name AS CustomerName
            FROM Invoices i
            LEFT JOIN Customers c ON i.CustomerId = c.Id
            WHERE i.Date BETWEEN @From AND @To
            ORDER BY i.Date DESC",
            new { From = from.ToString("yyyy-MM-dd"), To = to.ToString("yyyy-MM-dd 23:59:59") });
    }

    public async Task<IEnumerable<Invoice>> GetByCustomerAsync(int customerId)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<Invoice>(@"
            SELECT i.*, c.Name AS CustomerName
            FROM Invoices i
            LEFT JOIN Customers c ON i.CustomerId = c.Id
            WHERE i.CustomerId = @CustomerId
            ORDER BY i.Date DESC", new { CustomerId = customerId });
    }

    public async Task<int> AddAsync(Invoice invoice)
    {
        using var conn = _db.CreateConnection();
        using var transaction = conn.BeginTransaction();
        try
        {
            var invoiceId = await conn.ExecuteScalarAsync<int>(@"
                INSERT INTO Invoices (InvoiceNumber, CustomerId, Date, TotalAmount, TaxAmount,
                    DiscountAmount, PaymentStatus, PaymentMode)
                VALUES (@InvoiceNumber, @CustomerId, @Date, @TotalAmount, @TaxAmount,
                    @DiscountAmount, @PaymentStatus, @PaymentMode);
                SELECT last_insert_rowid();", invoice, transaction);

            foreach (var item in invoice.Items)
            {
                item.InvoiceId = invoiceId;
                await conn.ExecuteAsync(@"
                    INSERT INTO InvoiceItems (InvoiceId, ProductId, Quantity, Price, Discount)
                    VALUES (@InvoiceId, @ProductId, @Quantity, @Price, @Discount)", item, transaction);

                // Reduce stock
                await conn.ExecuteAsync(@"
                    UPDATE Products SET Stock = Stock - @Quantity, UpdatedAt = datetime('now')
                    WHERE Id = @ProductId", new { item.Quantity, item.ProductId }, transaction);
            }

            transaction.Commit();
            Logger.Information("Created invoice {InvoiceNumber} (ID: {Id})", invoice.InvoiceNumber, invoiceId);
            return invoiceId;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<bool> UpdateAsync(Invoice invoice)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync(@"
            UPDATE Invoices SET
                CustomerId = @CustomerId, TotalAmount = @TotalAmount,
                TaxAmount = @TaxAmount, DiscountAmount = @DiscountAmount,
                PaymentStatus = @PaymentStatus, PaymentMode = @PaymentMode
            WHERE Id = @Id", invoice);
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var conn = _db.CreateConnection();
        return await conn.ExecuteAsync("DELETE FROM Invoices WHERE Id = @Id", new { Id = id }) > 0;
    }

    public async Task<DashboardStats> GetDashboardStatsAsync()
    {
        using var conn = _db.CreateConnection();
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).ToString("yyyy-MM-dd");

        var stats = new DashboardStats
        {
            TotalProducts = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Products"),
            LowStockCount = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM Products WHERE Stock <= MinStockLevel"),
            TodaySalesCount = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM Invoices WHERE Date >= @Today", new { Today = today }),
            TodayRevenue = await conn.ExecuteScalarAsync<decimal>(
                "SELECT COALESCE(SUM(TotalAmount), 0) FROM Invoices WHERE Date >= @Today", new { Today = today }),
            MonthlyRevenue = await conn.ExecuteScalarAsync<decimal>(
                "SELECT COALESCE(SUM(TotalAmount), 0) FROM Invoices WHERE Date >= @MonthStart",
                new { MonthStart = monthStart }),
            TotalCustomers = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Customers"),
            TotalSuppliers = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Suppliers"),
        };

        // Last 7 days sales
        stats.Last7DaysSales = (await conn.QueryAsync<DailySales>(@"
            SELECT date(Date) as Date,
                   COALESCE(SUM(TotalAmount), 0) as Revenue,
                   COUNT(*) as OrderCount
            FROM Invoices
            WHERE Date >= date('now', '-7 days')
            GROUP BY date(Date)
            ORDER BY Date")).ToList();

        // Low stock products
        stats.LowStockProducts = (await conn.QueryAsync<Product>(@"
            SELECT * FROM Products
            WHERE Stock <= MinStockLevel
            ORDER BY Stock ASC
            LIMIT 10")).ToList();

        return stats;
    }

    public async Task<string> GenerateNextInvoiceNumberAsync()
    {
        using var conn = _db.CreateConnection();
        var lastNumber = await conn.ExecuteScalarAsync<string>(
            "SELECT InvoiceNumber FROM Invoices ORDER BY Id DESC LIMIT 1");

        if (string.IsNullOrEmpty(lastNumber))
            return "INV-0001";

        var numPart = lastNumber.Replace("INV-", "");
        if (int.TryParse(numPart, out var num))
            return $"INV-{(num + 1):D4}";

        return $"INV-{DateTime.UtcNow:yyyyMMddHHmmss}";
    }

    public async Task<IEnumerable<ProductSalesReport>> GetTopSellingProductsAsync(DateTime from, DateTime to, int count = 5)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<ProductSalesReport>(@"
            SELECT p.Id as ProductId, p.Name as ProductName,
                   SUM(ii.Quantity) as TotalQuantity,
                   SUM(ii.Price * ii.Quantity - ii.Discount) as TotalRevenue
            FROM InvoiceItems ii
            JOIN Invoices i ON ii.InvoiceId = i.Id
            JOIN Products p ON ii.ProductId = p.Id
            WHERE i.Date BETWEEN @From AND @To
            GROUP BY p.Id, p.Name
            ORDER BY TotalRevenue DESC
            LIMIT @Count",
            new { From = from.ToString("yyyy-MM-dd"), To = to.ToString("yyyy-MM-dd 23:59:59"), Count = count });
    }
}
