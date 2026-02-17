using Dapper;
using HardwareShopPro.Core.Interfaces;
using HardwareShopPro.Core.Models;
using HardwareShopPro.Data.Database;
using Serilog;

namespace HardwareShopPro.Data.Repositories;

/// <summary>
/// Product repository using Dapper with parameterized queries.
/// </summary>
public class ProductRepository : IProductRepository
{
    private readonly DatabaseContext _db;
    private static readonly ILogger Logger = Log.ForContext<ProductRepository>();

    public ProductRepository(DatabaseContext db) => _db = db;

    public async Task<IEnumerable<Product>> GetAllAsync()
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<Product>(@"
            SELECT p.*, s.Name AS SupplierName
            FROM Products p
            LEFT JOIN Suppliers s ON p.SupplierId = s.Id
            ORDER BY p.Name");
    }

    public async Task<Product?> GetByIdAsync(int id)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<Product>(@"
            SELECT p.*, s.Name AS SupplierName
            FROM Products p
            LEFT JOIN Suppliers s ON p.SupplierId = s.Id
            WHERE p.Id = @Id", new { Id = id });
    }

    public async Task<IEnumerable<Product>> SearchAsync(string searchTerm)
    {
        using var conn = _db.CreateConnection();
        var term = $"%{searchTerm}%";
        return await conn.QueryAsync<Product>(@"
            SELECT p.*, s.Name AS SupplierName
            FROM Products p
            LEFT JOIN Suppliers s ON p.SupplierId = s.Id
            WHERE p.Name LIKE @Term OR p.Brand LIKE @Term
               OR p.Category LIKE @Term OR p.SKU LIKE @Term
            ORDER BY p.Name", new { Term = term });
    }

    public async Task<IEnumerable<Product>> SearchByCriteriaAsync(SearchCriteria criteria)
    {
        using var conn = _db.CreateConnection();
        var sql = @"SELECT p.*, s.Name AS SupplierName
                    FROM Products p
                    LEFT JOIN Suppliers s ON p.SupplierId = s.Id
                    WHERE 1=1";
        var parameters = new DynamicParameters();

        if (!string.IsNullOrEmpty(criteria.Brand))
        {
            sql += " AND p.Brand LIKE @Brand";
            parameters.Add("Brand", $"%{criteria.Brand}%");
        }
        if (!string.IsNullOrEmpty(criteria.Category))
        {
            sql += " AND p.Category LIKE @Category";
            parameters.Add("Category", $"%{criteria.Category}%");
        }
        if (!string.IsNullOrEmpty(criteria.NameContains))
        {
            sql += " AND p.Name LIKE @Name";
            parameters.Add("Name", $"%{criteria.NameContains}%");
        }
        if (criteria.MaxPrice.HasValue)
        {
            sql += " AND p.SellingPrice <= @MaxPrice";
            parameters.Add("MaxPrice", criteria.MaxPrice.Value);
        }
        if (criteria.MinPrice.HasValue)
        {
            sql += " AND p.SellingPrice >= @MinPrice";
            parameters.Add("MinPrice", criteria.MinPrice.Value);
        }
        if (criteria.InStockOnly == true)
        {
            sql += " AND p.Stock > 0";
        }

        sql += " ORDER BY p.Name";
        return await conn.QueryAsync<Product>(sql, parameters);
    }

    public async Task<IEnumerable<Product>> GetByCategoryAsync(string category)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<Product>(@"
            SELECT p.*, s.Name AS SupplierName
            FROM Products p
            LEFT JOIN Suppliers s ON p.SupplierId = s.Id
            WHERE p.Category = @Category
            ORDER BY p.Name", new { Category = category });
    }

    public async Task<IEnumerable<Product>> GetLowStockAsync()
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<Product>(@"
            SELECT p.*, s.Name AS SupplierName
            FROM Products p
            LEFT JOIN Suppliers s ON p.SupplierId = s.Id
            WHERE p.Stock <= p.MinStockLevel
            ORDER BY p.Stock ASC");
    }

    public async Task<IEnumerable<string>> GetCategoriesAsync()
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<string>(
            "SELECT DISTINCT Category FROM Products WHERE Category != '' ORDER BY Category");
    }

    public async Task<IEnumerable<string>> GetBrandsAsync()
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<string>(
            "SELECT DISTINCT Brand FROM Products WHERE Brand != '' ORDER BY Brand");
    }

    public async Task<int> AddAsync(Product product)
    {
        using var conn = _db.CreateConnection();
        var id = await conn.ExecuteScalarAsync<int>(@"
            INSERT INTO Products (Name, Category, Brand, SKU, PurchasePrice, SellingPrice,
                Stock, MinStockLevel, SupplierId, LastRestockDate, CreatedAt, UpdatedAt)
            VALUES (@Name, @Category, @Brand, @SKU, @PurchasePrice, @SellingPrice,
                @Stock, @MinStockLevel, @SupplierId, @LastRestockDate, datetime('now'), datetime('now'));
            SELECT last_insert_rowid();", product);
        Logger.Information("Added product {Name} (ID: {Id})", product.Name, id);
        return id;
    }

    public async Task<bool> UpdateAsync(Product product)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync(@"
            UPDATE Products SET
                Name = @Name, Category = @Category, Brand = @Brand, SKU = @SKU,
                PurchasePrice = @PurchasePrice, SellingPrice = @SellingPrice,
                Stock = @Stock, MinStockLevel = @MinStockLevel,
                SupplierId = @SupplierId, LastRestockDate = @LastRestockDate,
                UpdatedAt = datetime('now')
            WHERE Id = @Id", product);
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync("DELETE FROM Products WHERE Id = @Id", new { Id = id });
        return rows > 0;
    }

    public async Task<int> GetTotalCountAsync()
    {
        using var conn = _db.CreateConnection();
        return await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Products");
    }
}
