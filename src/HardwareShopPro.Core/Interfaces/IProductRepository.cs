using HardwareShopPro.Core.Models;

namespace HardwareShopPro.Core.Interfaces;

/// <summary>
/// Repository for Product CRUD and queries.
/// All methods use parameterized queries to prevent SQL injection.
/// </summary>
public interface IProductRepository
{
    Task<IEnumerable<Product>> GetAllAsync();
    Task<Product?> GetByIdAsync(int id);
    Task<IEnumerable<Product>> SearchAsync(string searchTerm);
    Task<IEnumerable<Product>> SearchByCriteriaAsync(SearchCriteria criteria);
    Task<IEnumerable<Product>> GetByCategoryAsync(string category);
    Task<IEnumerable<Product>> GetLowStockAsync();
    Task<IEnumerable<string>> GetCategoriesAsync();
    Task<IEnumerable<string>> GetBrandsAsync();
    Task<int> AddAsync(Product product);
    Task<bool> UpdateAsync(Product product);
    Task<bool> DeleteAsync(int id);
    Task<int> GetTotalCountAsync();
}
