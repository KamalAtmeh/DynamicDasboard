using Dapper;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using DynamicDashboardCommon.Models;

namespace DynamicDasboardWebAPI.Repositories
{
    /// <summary>
    /// Repository class for managing user data in the database.
    /// Provides methods to perform CRUD operations on the Users table.
    /// </summary>
    public class UserRepository
    {
        private readonly IDbConnection _connection;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserRepository"/> class.
        /// </summary>
        /// <param name="connection">The database connection to be used by the repository.</param>
        public UserRepository(IDbConnection connection)
        {
            _connection = connection;
        }

        /// <summary>
        /// Retrieves all users from the database.
        /// </summary>
        /// <returns>A collection of <see cref="User"/> objects.</returns>
        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            string query = "SELECT * FROM Users";
            return await _connection.QueryAsync<User>(query);
        }

        /// <summary>
        /// Adds a new user to the database.
        /// </summary>
        /// <param name="user">The <see cref="User"/> object to be added.</param>
        /// <returns>The number of rows affected.</returns>
        public async Task<int> AddUserAsync(User user)
        {
            string query = "INSERT INTO Users (Username, RoleID, AllowedDatabases, PasswordHash , CreatedAt) VALUES (@Username, @RoleID, @AllowedDatabases, @PasswordHash, @CreatedAt)";
            return await _connection.ExecuteAsync(query, user);
        }

        /// <summary>
        /// Retrieves a user from the database by their ID.
        /// </summary>
        /// <param name="userId">The ID of the user to be retrieved.</param>
        /// <returns>The <see cref="User"/> object if found; otherwise, null.</returns>
        public async Task<User> GetUserByIDAsync(int userId)
        {
            string query = "SELECT * FROM Users WHERE UserID = @UserID";
            return await _connection.QueryFirstOrDefaultAsync<User>(query, new { UserID = userId });
        }
    }
}
