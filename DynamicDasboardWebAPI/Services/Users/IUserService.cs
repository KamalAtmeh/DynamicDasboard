using DynamicDashboardCommon.Models;

namespace DynamicDasboardWebAPI.Services
{
    public interface IUserService
    {
        /// <summary>
        /// Retrieves all users asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains a collection of users.</returns>
        Task<IEnumerable<User>> GetAllUsersAsync();

        /// <summary>
        /// Adds a new user asynchronously.
        /// </summary>
        /// <param name="user">The user to add.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the ID of the added user.</returns>
        Task<int> AddUserAsync(User user);

        /// <summary>
        /// Retrieves a user by their ID asynchronously.
        /// </summary>
        /// <param name="userId">The ID of the user to retrieve.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the user with the specified ID.</returns>
        Task<User> GetUserByIDAsync(int userId);
    }
}
