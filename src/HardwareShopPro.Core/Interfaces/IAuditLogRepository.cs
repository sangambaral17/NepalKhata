using HardwareShopPro.Core.Models;

namespace HardwareShopPro.Core.Interfaces;

/// <summary>
/// Repository for audit trail logging.
/// </summary>
public interface IAuditLogRepository
{
    Task AddAsync(AuditLog log);
    Task<IEnumerable<AuditLog>> GetByDateRangeAsync(DateTime from, DateTime to);
    Task<IEnumerable<AuditLog>> GetByUserAsync(int userId);
    Task<IEnumerable<AuditLog>> GetRecentAsync(int count = 50);
}
