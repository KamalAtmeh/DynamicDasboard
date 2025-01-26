using DynamicDashboardCommon.Models;

namespace DynamicDasboardWebAPI.Services
{
    public interface IUserService
    {
        Task<IEnumerable<User>> GetAllUsersAsync();
        Task<int> AddUserAsync(User user);
        Task<User> GetUserByIDAsync(int userId);
    }
}
