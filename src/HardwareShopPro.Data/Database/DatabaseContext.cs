using Microsoft.Data.Sqlite;
using Dapper;
using Serilog;

namespace HardwareShopPro.Data.Database;

/// <summary>
/// Manages SQLite database connection and schema initialization.
/// Uses WAL mode for better concurrent read performance.
/// </summary>
public class DatabaseContext
{
    private readonly string _connectionString;
    private static readonly ILogger Logger = Log.ForContext<DatabaseContext>();

    public DatabaseContext(string databasePath)
    {
        // Ensure the directory exists
        var dir = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        _connectionString = $"Data Source={databasePath}";
    }

    /// <summary>
    /// Creates a new open SQLite connection.
    /// Caller is responsible for disposing.
    /// </summary>
    public SqliteConnection CreateConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }

    /// <summary>
    /// Initializes the database: enables WAL mode and creates all tables.
    /// Safe to call multiple times (uses IF NOT EXISTS).
    /// </summary>
    public async Task InitializeAsync()
    {
        Logger.Information("Initializing database...");

        using var connection = CreateConnection();

        // Enable WAL mode for better read performance
        await connection.ExecuteAsync("PRAGMA journal_mode=WAL;");
        await connection.ExecuteAsync("PRAGMA foreign_keys=ON;");

        // Create all tables
        await connection.ExecuteAsync(CreateUsersTable);
        await connection.ExecuteAsync(CreateSuppliersTable);
        await connection.ExecuteAsync(CreateCustomersTable);
        await connection.ExecuteAsync(CreateProductsTable);
        await connection.ExecuteAsync(CreateInvoicesTable);
        await connection.ExecuteAsync(CreateInvoiceItemsTable);
        await connection.ExecuteAsync(CreateAuditLogTable);

        Logger.Information("Database initialized successfully.");
    }

    #region Table Definitions

    private const string CreateUsersTable = @"
        CREATE TABLE IF NOT EXISTS Users (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Username TEXT NOT NULL UNIQUE COLLATE NOCASE,
            PasswordHash TEXT NOT NULL,
            DisplayName TEXT NOT NULL,
            Role INTEGER NOT NULL DEFAULT 0,
            IsActive INTEGER NOT NULL DEFAULT 1,
            CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
            LastLoginAt TEXT
        );";

    private const string CreateSuppliersTable = @"
        CREATE TABLE IF NOT EXISTS Suppliers (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Name TEXT NOT NULL,
            Contact TEXT NOT NULL DEFAULT '',
            Email TEXT NOT NULL DEFAULT '',
            Address TEXT NOT NULL DEFAULT '',
            GSTIN TEXT NOT NULL DEFAULT '',
            CreatedAt TEXT NOT NULL DEFAULT (datetime('now'))
        );";

    private const string CreateCustomersTable = @"
        CREATE TABLE IF NOT EXISTS Customers (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Name TEXT NOT NULL,
            Phone TEXT NOT NULL DEFAULT '',
            Email TEXT NOT NULL DEFAULT '',
            Address TEXT NOT NULL DEFAULT '',
            GSTIN TEXT NOT NULL DEFAULT '',
            CreatedAt TEXT NOT NULL DEFAULT (datetime('now'))
        );";

    private const string CreateProductsTable = @"
        CREATE TABLE IF NOT EXISTS Products (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Name TEXT NOT NULL,
            Category TEXT NOT NULL DEFAULT '',
            Brand TEXT NOT NULL DEFAULT '',
            SKU TEXT NOT NULL DEFAULT '' UNIQUE,
            PurchasePrice REAL NOT NULL DEFAULT 0,
            SellingPrice REAL NOT NULL DEFAULT 0,
            Stock INTEGER NOT NULL DEFAULT 0,
            MinStockLevel INTEGER NOT NULL DEFAULT 5,
            SupplierId INTEGER,
            LastRestockDate TEXT,
            CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
            UpdatedAt TEXT NOT NULL DEFAULT (datetime('now')),
            FOREIGN KEY (SupplierId) REFERENCES Suppliers(Id) ON DELETE SET NULL
        );
        CREATE INDEX IF NOT EXISTS IX_Products_Category ON Products(Category);
        CREATE INDEX IF NOT EXISTS IX_Products_Brand ON Products(Brand);
        CREATE INDEX IF NOT EXISTS IX_Products_SKU ON Products(SKU);";

    private const string CreateInvoicesTable = @"
        CREATE TABLE IF NOT EXISTS Invoices (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            InvoiceNumber TEXT NOT NULL UNIQUE,
            CustomerId INTEGER,
            Date TEXT NOT NULL DEFAULT (datetime('now')),
            TotalAmount REAL NOT NULL DEFAULT 0,
            TaxAmount REAL NOT NULL DEFAULT 0,
            DiscountAmount REAL NOT NULL DEFAULT 0,
            PaymentStatus INTEGER NOT NULL DEFAULT 0,
            PaymentMode INTEGER NOT NULL DEFAULT 0,
            CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
            FOREIGN KEY (CustomerId) REFERENCES Customers(Id) ON DELETE SET NULL
        );
        CREATE INDEX IF NOT EXISTS IX_Invoices_Date ON Invoices(Date);
        CREATE INDEX IF NOT EXISTS IX_Invoices_CustomerId ON Invoices(CustomerId);";

    private const string CreateInvoiceItemsTable = @"
        CREATE TABLE IF NOT EXISTS InvoiceItems (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            InvoiceId INTEGER NOT NULL,
            ProductId INTEGER NOT NULL,
            Quantity INTEGER NOT NULL DEFAULT 1,
            Price REAL NOT NULL DEFAULT 0,
            Discount REAL NOT NULL DEFAULT 0,
            FOREIGN KEY (InvoiceId) REFERENCES Invoices(Id) ON DELETE CASCADE,
            FOREIGN KEY (ProductId) REFERENCES Products(Id) ON DELETE RESTRICT
        );
        CREATE INDEX IF NOT EXISTS IX_InvoiceItems_InvoiceId ON InvoiceItems(InvoiceId);";

    private const string CreateAuditLogTable = @"
        CREATE TABLE IF NOT EXISTS AuditLog (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            UserId INTEGER NOT NULL,
            Username TEXT NOT NULL,
            Action TEXT NOT NULL,
            Entity TEXT NOT NULL,
            EntityId INTEGER,
            Details TEXT,
            Timestamp TEXT NOT NULL DEFAULT (datetime('now'))
        );
        CREATE INDEX IF NOT EXISTS IX_AuditLog_Timestamp ON AuditLog(Timestamp);
        CREATE INDEX IF NOT EXISTS IX_AuditLog_UserId ON AuditLog(UserId);";

    #endregion
}
