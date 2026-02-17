using Dapper;
using HardwareShopPro.Core.Interfaces;
using HardwareShopPro.Core.Models;
using HardwareShopPro.Data.Database;

namespace HardwareShopPro.Data.Repositories;

public class UserRepository : IUserRepository
{
    private readonly DatabaseContext _db;
    public UserRepository(DatabaseContext db) => _db = db;

    public async Task<User?> GetByUsernameAsync(string username)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<User>(
            "SELECT * FROM Users WHERE Username = @Username COLLATE NOCASE",
            new { Username = username });
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<User>(
            "SELECT * FROM Users WHERE Id = @Id", new { Id = id });
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<User>("SELECT * FROM Users ORDER BY Username");
    }

    public async Task<int> AddAsync(User user)
    {
        using var conn = _db.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(@"
            INSERT INTO Users (Username, PasswordHash, DisplayName, Role, IsActive)
            VALUES (@Username, @PasswordHash, @DisplayName, @Role, @IsActive);
            SELECT last_insert_rowid();", user);
    }

    public async Task<bool> UpdateAsync(User user)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync(@"
            UPDATE Users SET DisplayName = @DisplayName, Role = @Role, IsActive = @IsActive
            WHERE Id = @Id", user);
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var conn = _db.CreateConnection();
        return await conn.ExecuteAsync("DELETE FROM Users WHERE Id = @Id", new { Id = id }) > 0;
    }

    public async Task UpdateLastLoginAsync(int userId)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE Users SET LastLoginAt = datetime('now') WHERE Id = @Id",
            new { Id = userId });
    }
}
