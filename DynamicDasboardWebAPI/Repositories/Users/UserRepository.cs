using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using DynamicDashboardCommon.Models;
using Microsoft.Extensions.Logging;
using DynamicDasboardWebAPI.Utilities;

namespace DynamicDasboardWebAPI.Repositories
{
    public class UserRepository
    {
        private readonly IDbConnection _connection;
        private readonly DbConnectionFactory _connectionFactory;

        public UserRepository(
                   IDbConnection connection,
                   DbConnectionFactory connectionFactory)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            try
            {
                const string query = "SELECT * FROM Users";
                return await _connection.QuerySafeAsync<User>(query);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<int> AddUserAsync(User user)
        {
            try
            {


                const string query = @"
                    INSERT INTO Users 
                    (Username, RoleID, AllowedDatabases, PasswordHash, CreatedAt) 
                    VALUES 
                    (@Username, @RoleID, @AllowedDatabases, @PasswordHash, @CreatedAt);
                    SELECT CAST(SCOPE_IDENTITY() as int)";

                return await _connection.ExecuteScalarSafeAsync<int>(query, user);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<User> GetUserByIDAsync(int userId)
        {
            try
            {
                const string query = "SELECT * FROM Users WHERE UserID = @UserID";
                return await _connection.QueryFirstOrDefaultSafeAsync<User>(query, new { UserID = userId });
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<User> GetUserByUsernameAsync(string username)
        {
            try
            {
                const string query = "SELECT * FROM Users WHERE Username = @Username";
                return await _connection.QueryFirstOrDefaultSafeAsync<User>(query, new { Username = username });
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<int> UpdateUserAsync(User user)
        {
            try
            {
                const string query = @"
                    UPDATE Users 
                    SET Username = @Username, 
                        RoleID = @RoleID, 
                        AllowedDatabases = @AllowedDatabases, 
                        PasswordHash = @PasswordHash
                    WHERE UserID = @UserID";

                return await _connection.ExecuteSafeAsync(query, user);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<int> DeleteUserAsync(int userId)
        {
            try
            {
                const string query = "DELETE FROM Users WHERE UserID = @UserID";
                return await _connection.ExecuteSafeAsync(query, new { UserID = userId });
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}