using Dapper;
using HardwareShopPro.Core.Interfaces;
using HardwareShopPro.Core.Models;
using HardwareShopPro.Data.Database;

namespace HardwareShopPro.Data.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly DatabaseContext _db;
    public CustomerRepository(DatabaseContext db) => _db = db;

    public async Task<IEnumerable<Customer>> GetAllAsync()
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<Customer>("SELECT * FROM Customers ORDER BY Name");
    }

    public async Task<Customer?> GetByIdAsync(int id)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<Customer>(
            "SELECT * FROM Customers WHERE Id = @Id", new { Id = id });
    }

    public async Task<IEnumerable<Customer>> SearchAsync(string searchTerm)
    {
        using var conn = _db.CreateConnection();
        var term = $"%{searchTerm}%";
        return await conn.QueryAsync<Customer>(@"
            SELECT * FROM Customers
            WHERE Name LIKE @Term OR Phone LIKE @Term OR Email LIKE @Term
            ORDER BY Name", new { Term = term });
    }

    public async Task<int> AddAsync(Customer customer)
    {
        using var conn = _db.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(@"
            INSERT INTO Customers (Name, Phone, Email, Address, GSTIN)
            VALUES (@Name, @Phone, @Email, @Address, @GSTIN);
            SELECT last_insert_rowid();", customer);
    }

    public async Task<bool> UpdateAsync(Customer customer)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync(@"
            UPDATE Customers SET
                Name = @Name, Phone = @Phone, Email = @Email,
                Address = @Address, GSTIN = @GSTIN
            WHERE Id = @Id", customer);
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var conn = _db.CreateConnection();
        return await conn.ExecuteAsync("DELETE FROM Customers WHERE Id = @Id", new { Id = id }) > 0;
    }
}
