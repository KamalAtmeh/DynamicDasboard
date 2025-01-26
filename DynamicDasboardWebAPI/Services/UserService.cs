using System.Collections.Generic;
using System.Threading.Tasks;
using DynamicDasboardWebAPI.Repositories;
using DynamicDashboardCommon.Models;

namespace DynamicDasboardWebAPI.Services
{

    public class UserService : IUserService
    {
        private readonly UserRepository _repository;

        public UserService(UserRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            return await _repository.GetAllUsersAsync();
        }

        public async Task<int> AddUserAsync(User user)
        {
            if (string.IsNullOrEmpty(user.Username))
            {
                throw new ArgumentException("Username cannot be empty");
            }

            // Add validation logic for RoleID or other fields if needed.
            return await _repository.AddUserAsync(user);
        }

        public async Task<User> GetUserByIDAsync(int userId)
        {
            return await _repository.GetUserByIDAsync(userId);
        }
    }
}
