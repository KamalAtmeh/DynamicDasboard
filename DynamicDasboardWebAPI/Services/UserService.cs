using System.Collections.Generic;
using System.Threading.Tasks;
using DynamicDasboardWebAPI.Repositories;
using DynamicDashboardCommon.Models;

namespace DynamicDasboardWebAPI.Services
{

    /// <summary>
    /// Service class for managing user-related operations.
    /// </summary>
    public class UserService : IUserService
    {
        private readonly UserRepository _repository;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserService"/> class.
        /// </summary>
        /// <param name="repository">The user repository instance.</param>
        public UserService(UserRepository repository)
        {
            _repository = repository;
        }

        /// <summary>
        /// Retrieves all users asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains a collection of users.</returns>
        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            return await _repository.GetAllUsersAsync();
        }

        /// <summary>
        /// Adds a new user asynchronously.
        /// </summary>
        /// <param name="user">The user to add.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the ID of the added user.</returns>
        /// <exception cref="ArgumentException">Thrown when the username is null or empty.</exception>
        public async Task<int> AddUserAsync(User user)
        {
            if (string.IsNullOrEmpty(user.Username))
            {
                throw new ArgumentException("Username cannot be empty");
            }

            // Add validation logic for RoleID or other fields if needed.
            return await _repository.AddUserAsync(user);
        }

        /// <summary>
        /// Retrieves a user by their ID asynchronously.
        /// </summary>
        /// <param name="userId">The ID of the user to retrieve.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the user with the specified ID.</returns>
        public async Task<User> GetUserByIDAsync(int userId)
        {
            return await _repository.GetUserByIDAsync(userId);
        }
    }
}
