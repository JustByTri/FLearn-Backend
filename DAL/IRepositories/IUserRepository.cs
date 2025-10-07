using DAL.Basic;
using DAL.Models;

namespace DAL.IRepositories
{
    public interface IUserRepository : IGenericRepository<User>
    {
        Task<User> GetByEmailAsync(string email);
        Task<User> GetByUsernameAsync(string username);
        Task<List<User>> GetUsersByRoleAsync(string roleName);
        Task<bool> IsEmailExistsAsync(string email);
        Task<bool> IsUsernameExistsAsync(string username);
        Task<User> GetUserWithRolesAsync(Guid userId);
        Task<List<User>> GetActiveUsersAsync();
        Task<List<User>> GetUsersByIndustryAsync(string industry);
        Task<List<User>> GetAllUsersWithRolesAsync();
        Task<int> GetTotalUsersCountAsync();
        Task<int> GetActiveUsersCountAsync();
        Task<int> GetUsersCountByRoleAsync(string roleName);
        Task<List<User>> GetRecentUsersAsync(int count);
    }
}
