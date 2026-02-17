using HardwareShopPro.Core.Models;

namespace HardwareShopPro.Core.Interfaces;

/// <summary>
/// Repository for user authentication and management.
/// </summary>
public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByIdAsync(int id);
    Task<IEnumerable<User>> GetAllAsync();
    Task<int> AddAsync(User user);
    Task<bool> UpdateAsync(User user);
    Task<bool> DeleteAsync(int id);
    Task UpdateLastLoginAsync(int userId);
}
