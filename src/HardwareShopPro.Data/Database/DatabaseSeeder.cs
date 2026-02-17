using Dapper;
using HardwareShopPro.Data.Database;
using Serilog;

namespace HardwareShopPro.Data;

/// <summary>
/// Seeds the database with sample data for development and testing.
/// Only seeds if tables are empty to avoid duplicates.
/// </summary>
public class DatabaseSeeder
{
    private readonly DatabaseContext _db;
    private static readonly ILogger Logger = Log.ForContext<DatabaseSeeder>();

    public DatabaseSeeder(DatabaseContext db) => _db = db;

    public async Task SeedAsync()
    {
        using var conn = _db.CreateConnection();

        // Only seed if no data exists
        var userCount = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Users");
        if (userCount > 0)
        {
            Logger.Information("Database already seeded, skipping.");
            return;
        }

        Logger.Information("Seeding database with sample data...");

        // Default admin user (password: admin123)
        await conn.ExecuteAsync(@"
            INSERT INTO Users (Username, PasswordHash, DisplayName, Role, IsActive)
            VALUES (@Username, @PasswordHash, @DisplayName, @Role, 1)",
            new
            {
                Username = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                DisplayName = "System Administrator",
                Role = 2 // Admin
            });

        // Manager user (password: manager123)
        await conn.ExecuteAsync(@"
            INSERT INTO Users (Username, PasswordHash, DisplayName, Role, IsActive)
            VALUES (@Username, @PasswordHash, @DisplayName, @Role, 1)",
            new
            {
                Username = "manager",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("manager123"),
                DisplayName = "Shop Manager",
                Role = 1 // Manager
            });

        // Cashier user (password: cashier123)
        await conn.ExecuteAsync(@"
            INSERT INTO Users (Username, PasswordHash, DisplayName, Role, IsActive)
            VALUES (@Username, @PasswordHash, @DisplayName, @Role, 1)",
            new
            {
                Username = "cashier",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("cashier123"),
                DisplayName = "Front Desk Cashier",
                Role = 0 // Cashier
            });

        // ─── Suppliers ─────────────────────────────────────────
        await conn.ExecuteAsync(@"
            INSERT INTO Suppliers (Name, Contact, Email, Address, GSTIN) VALUES
            ('TechDistrib Nepal', '9801234567', 'sales@techdistrib.np', 'New Road, Kathmandu', '12ABCDE1234F1Z5'),
            ('Himalayan Components', '9807654321', 'info@himcomp.np', 'Putalisadak, Kathmandu', '27AABCU9603R1ZM'),
            ('Silicon Valley Imports', '9841112222', 'orders@svimports.np', 'Durbar Marg, Kathmandu', '29AADCS1234P1ZN'),
            ('Dragon Electronics', '9856667777', 'dragon@electro.np', 'Bagbazar, Kathmandu', '07AAGCD5678Q1ZR'),
            ('Nepal PC Parts', '9823334444', 'parts@nepalpc.np', 'Naxal, Kathmandu', '33AACFN9876S1ZT')");

        // ─── Customers ─────────────────────────────────────────
        await conn.ExecuteAsync(@"
            INSERT INTO Customers (Name, Phone, Email, Address, GSTIN) VALUES
            ('Ram Sharma', '9841001001', 'ram@email.com', 'Lazimpat, Kathmandu', ''),
            ('Sita Thapa', '9851002002', 'sita@email.com', 'Baluwatar, Kathmandu', '12XYZAB5678C1D2'),
            ('Hari Bahadur', '9861003003', 'hari@email.com', 'Patan, Lalitpur', ''),
            ('Gita Shrestha', '9871004004', 'gita@email.com', 'Bhaktapur', '27DEFGH9012E3F4'),
            ('Cyber Cafe Express', '9881005005', 'cybercafe@email.com', 'Thamel, Kathmandu', '29IJKLM3456G5H6')");

        // ─── Products (20 hardware items) ──────────────────────
        var products = new[]
        {
            ("Corsair Vengeance 16GB DDR4 RAM", "RAM", "Corsair", "RAM-COR-16D4", 4500m, 5800m, 25, 5, 1),
            ("Kingston Fury 8GB DDR4 RAM", "RAM", "Kingston", "RAM-KIN-8D4", 2200m, 3200m, 40, 10, 1),
            ("Samsung 970 EVO 500GB NVMe SSD", "SSD", "Samsung", "SSD-SAM-500N", 5500m, 7500m, 15, 3, 2),
            ("WD Blue 1TB SATA SSD", "SSD", "Western Digital", "SSD-WDB-1TS", 6000m, 8200m, 12, 3, 2),
            ("Seagate Barracuda 2TB HDD", "HDD", "Seagate", "HDD-SEA-2TB", 4800m, 6500m, 20, 5, 3),
            ("NVIDIA GeForce RTX 4060 8GB", "GPU", "NVIDIA", "GPU-NV-4060", 28000m, 35000m, 8, 2, 3),
            ("AMD Radeon RX 7600 8GB", "GPU", "AMD", "GPU-AMD-7600", 22000m, 28500m, 10, 2, 3),
            ("Intel Core i5-13400F Processor", "CPU", "Intel", "CPU-INT-13400", 15000m, 19500m, 12, 3, 4),
            ("AMD Ryzen 5 5600X Processor", "CPU", "AMD", "CPU-AMD-5600X", 12000m, 16000m, 18, 4, 4),
            ("ASUS ROG B550-F Gaming Motherboard", "Motherboard", "ASUS", "MB-ASUS-B550", 14000m, 18500m, 7, 2, 5),
            ("Gigabyte B660M DS3H Motherboard", "Motherboard", "Gigabyte", "MB-GIG-B660", 8500m, 11500m, 15, 3, 5),
            ("Corsair RM750 Power Supply 750W", "PSU", "Corsair", "PSU-COR-750", 7000m, 9500m, 10, 3, 1),
            ("Cooler Master MasterBox Q300L Case", "Case", "Cooler Master", "CASE-CM-Q300", 4500m, 6200m, 14, 3, 2),
            ("Logitech G502 HERO Gaming Mouse", "Peripherals", "Logitech", "PER-LOG-G502", 3200m, 4500m, 30, 5, 1),
            ("HyperX Alloy Origins 60 Keyboard", "Peripherals", "HyperX", "PER-HYP-KB60", 5500m, 7800m, 20, 4, 2),
            ("HDMI Cable 2.0 3M", "Cables", "Generic", "CBL-HDMI-3M", 250m, 500m, 100, 20, 4),
            ("Cat6 Ethernet Cable 5M", "Cables", "Generic", "CBL-CAT6-5M", 150m, 350m, 80, 15, 4),
            ("TP-Link Archer AX55 WiFi Router", "Networking", "TP-Link", "NET-TPL-AX55", 5000m, 7200m, 9, 2, 5),
            ("LG 24MP400 24\" IPS Monitor", "Monitor", "LG", "MON-LG-24MP", 12000m, 16000m, 6, 2, 3),
            ("Thermal Paste Arctic MX-6 4g", "Accessories", "Arctic", "ACC-ARC-MX6", 400m, 800m, 50, 10, 1)
        };

        foreach (var (name, cat, brand, sku, buy, sell, stock, min, suppId) in products)
        {
            await conn.ExecuteAsync(@"
                INSERT INTO Products (Name, Category, Brand, SKU, PurchasePrice, SellingPrice,
                    Stock, MinStockLevel, SupplierId, LastRestockDate)
                VALUES (@Name, @Cat, @Brand, @SKU, @Buy, @Sell, @Stock, @Min, @SuppId, datetime('now', '-' || abs(random() % 30) || ' days'))",
                new { Name = name, Cat = cat, Brand = brand, SKU = sku, Buy = buy, Sell = sell, Stock = stock, Min = min, SuppId = suppId });
        }

        // ─── Sample Invoices ───────────────────────────────────
        var rng = new Random(42);
        for (int i = 1; i <= 15; i++)
        {
            var invoiceNum = $"INV-{i:D4}";
            var customerId = rng.Next(1, 6);
            var daysAgo = rng.Next(0, 14);
            var paymentStatus = rng.Next(0, 3);
            var paymentMode = rng.Next(0, 4);

            // Pick 1-3 random products for each invoice
            var itemCount = rng.Next(1, 4);
            decimal total = 0;
            var items = new List<(int productId, int qty, decimal price)>();
            for (int j = 0; j < itemCount; j++)
            {
                var pId = rng.Next(1, 21);
                var qty = rng.Next(1, 5);
                var price = products[pId - 1].Item6; // selling price
                total += price * qty;
                items.Add((pId, qty, price));
            }

            var tax = Math.Round(total * 0.13m, 2); // 13% GST
            var invoiceId = await conn.ExecuteScalarAsync<int>(@"
                INSERT INTO Invoices (InvoiceNumber, CustomerId, Date, TotalAmount, TaxAmount,
                    DiscountAmount, PaymentStatus, PaymentMode)
                VALUES (@Num, @CustId, datetime('now', @Days), @Total, @Tax, 0, @PS, @PM);
                SELECT last_insert_rowid();",
                new { Num = invoiceNum, CustId = customerId, Days = $"-{daysAgo} days", Total = total, Tax = tax, PS = paymentStatus, PM = paymentMode });

            foreach (var (productId, qty, price) in items)
            {
                await conn.ExecuteAsync(@"
                    INSERT INTO InvoiceItems (InvoiceId, ProductId, Quantity, Price, Discount)
                    VALUES (@InvId, @ProdId, @Qty, @Price, 0)",
                    new { InvId = invoiceId, ProdId = productId, Qty = qty, Price = price });
            }
        }

        Logger.Information("Database seeded: 3 users, 5 suppliers, 5 customers, 20 products, 15 invoices.");
    }
}
