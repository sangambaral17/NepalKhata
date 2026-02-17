using Dapper;
using HardwareShopPro.Core.Interfaces;
using HardwareShopPro.Core.Models;
using HardwareShopPro.Data.Database;

namespace HardwareShopPro.Data.Repositories;

public class SupplierRepository : ISupplierRepository
{
    private readonly DatabaseContext _db;
    public SupplierRepository(DatabaseContext db) => _db = db;

    public async Task<IEnumerable<Supplier>> GetAllAsync()
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<Supplier>("SELECT * FROM Suppliers ORDER BY Name");
    }

    public async Task<Supplier?> GetByIdAsync(int id)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<Supplier>(
            "SELECT * FROM Suppliers WHERE Id = @Id", new { Id = id });
    }

    public async Task<IEnumerable<Supplier>> SearchAsync(string searchTerm)
    {
        using var conn = _db.CreateConnection();
        var term = $"%{searchTerm}%";
        return await conn.QueryAsync<Supplier>(@"
            SELECT * FROM Suppliers
            WHERE Name LIKE @Term OR Contact LIKE @Term OR Email LIKE @Term
            ORDER BY Name", new { Term = term });
    }

    public async Task<int> AddAsync(Supplier supplier)
    {
        using var conn = _db.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(@"
            INSERT INTO Suppliers (Name, Contact, Email, Address, GSTIN)
            VALUES (@Name, @Contact, @Email, @Address, @GSTIN);
            SELECT last_insert_rowid();", supplier);
    }

    public async Task<bool> UpdateAsync(Supplier supplier)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync(@"
            UPDATE Suppliers SET
                Name = @Name, Contact = @Contact, Email = @Email,
                Address = @Address, GSTIN = @GSTIN
            WHERE Id = @Id", supplier);
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var conn = _db.CreateConnection();
        return await conn.ExecuteAsync("DELETE FROM Suppliers WHERE Id = @Id", new { Id = id }) > 0;
    }
}
