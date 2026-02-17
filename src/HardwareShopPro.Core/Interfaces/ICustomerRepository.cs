using HardwareShopPro.Core.Models;

namespace HardwareShopPro.Core.Interfaces;

/// <summary>
/// Repository for Customer CRUD and queries.
/// </summary>
public interface ICustomerRepository
{
    Task<IEnumerable<Customer>> GetAllAsync();
    Task<Customer?> GetByIdAsync(int id);
    Task<IEnumerable<Customer>> SearchAsync(string searchTerm);
    Task<int> AddAsync(Customer customer);
    Task<bool> UpdateAsync(Customer customer);
    Task<bool> DeleteAsync(int id);
}
