using HardwareShopPro.Core.Models;

namespace HardwareShopPro.Core.Interfaces;

/// <summary>
/// Repository for Invoice operations including dashboard statistics.
/// </summary>
public interface IInvoiceRepository
{
    Task<IEnumerable<Invoice>> GetAllAsync();
    Task<Invoice?> GetByIdAsync(int id);
    Task<IEnumerable<Invoice>> GetByDateRangeAsync(DateTime from, DateTime to);
    Task<IEnumerable<Invoice>> GetByCustomerAsync(int customerId);
    Task<int> AddAsync(Invoice invoice);
    Task<bool> UpdateAsync(Invoice invoice);
    Task<bool> DeleteAsync(int id);
    Task<DashboardStats> GetDashboardStatsAsync();
    Task<string> GenerateNextInvoiceNumberAsync();
}
