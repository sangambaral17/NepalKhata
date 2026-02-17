using Dapper;
using HardwareShopPro.Core.Interfaces;
using HardwareShopPro.Core.Models;
using HardwareShopPro.Data.Database;

namespace HardwareShopPro.Data.Repositories;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly DatabaseContext _db;
    public AuditLogRepository(DatabaseContext db) => _db = db;

    public async Task AddAsync(AuditLog log)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(@"
            INSERT INTO AuditLog (UserId, Username, Action, Entity, EntityId, Details, Timestamp)
            VALUES (@UserId, @Username, @Action, @Entity, @EntityId, @Details, datetime('now'))", log);
    }

    public async Task<IEnumerable<AuditLog>> GetByDateRangeAsync(DateTime from, DateTime to)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<AuditLog>(@"
            SELECT * FROM AuditLog
            WHERE Timestamp BETWEEN @From AND @To
            ORDER BY Timestamp DESC",
            new { From = from.ToString("yyyy-MM-dd"), To = to.ToString("yyyy-MM-dd 23:59:59") });
    }

    public async Task<IEnumerable<AuditLog>> GetByUserAsync(int userId)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<AuditLog>(@"
            SELECT * FROM AuditLog WHERE UserId = @UserId ORDER BY Timestamp DESC",
            new { UserId = userId });
    }

    public async Task<IEnumerable<AuditLog>> GetRecentAsync(int count = 50)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<AuditLog>(@"
            SELECT * FROM AuditLog ORDER BY Timestamp DESC LIMIT @Count",
            new { Count = count });
    }
}
