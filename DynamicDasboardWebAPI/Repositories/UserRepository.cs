using Dapper;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using DynamicDashboardCommon.Models;

namespace DynamicDasboardWebAPI.Repositories
{
    public class UserRepository
    {
        private readonly IDbConnection _connection;

        public UserRepository(IDbConnection connection)
        {
            _connection = connection;
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            string query = "SELECT * FROM Users";
            return await _connection.QueryAsync<User>(query);
        }

        public async Task<int> AddUserAsync(User user)
        {
            string query = "INSERT INTO Users (Username, RoleID, AllowedDatabases, PasswordHash , CreatedAt) VALUES (@Username, @RoleID, @AllowedDatabases, @PasswordHash, @CreatedAt)";
            return await _connection.ExecuteAsync(query, user);
        }

        public async Task<User> GetUserByIDAsync(int userId)
        {
            string query = "SELECT * FROM Users WHERE UserID = @UserID";
            return await _connection.QueryFirstOrDefaultAsync<User>(query, new { UserID = userId });
        }
    }
}
