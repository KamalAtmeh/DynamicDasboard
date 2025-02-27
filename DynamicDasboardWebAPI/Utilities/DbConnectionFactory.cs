using System;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using MySql.Data.MySqlClient;
using Oracle.ManagedDataAccess.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DynamicDasboardWebAPI.Utilities
{
    public class DbConnectionFactory
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<DbConnectionFactory> _logger;

        public DbConnectionFactory(IConfiguration configuration, ILogger<DbConnectionFactory> logger = null)
        {
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Creates a database connection based on the specified database type.
        /// </summary>
        /// <param name="dbType">Type of the database (e.g., SQLServer, MySQL, Oracle).</param>
        /// <returns>An <see cref="IDbConnection"/> instance.</returns>
        public IDbConnection CreateConnection(string dbType)
        {
            string connectionString = GetConnectionString(dbType);
            return dbType switch
            {
                "SQLServer" => new SqlConnection(connectionString),
                "MySQL" => new MySqlConnection(connectionString),
                "Oracle" => new OracleConnection(connectionString),
                "SQLServer2" => new OracleConnection(connectionString),
                _ => throw new ArgumentException($"Unsupported database type: {dbType}")
            };
        }

        /// <summary>
        /// Creates and opens a database connection asynchronously based on the specified database type.
        /// </summary>
        /// <param name="dbType">Type of the database (e.g., SQLServer, MySQL, Oracle).</param>
        /// <returns>An opened <see cref="IDbConnection"/> instance.</returns>
        public async Task<IDbConnection> CreateOpenConnectionAsync(string dbType)
        {
            string connectionString = GetConnectionString(dbType);

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Connection string is missing or invalid.");

            IDbConnection connection = null;

            try
            {
                switch (dbType)
                {//temp implementation to be changed
                    case "SQLServer":
                        var sqlConnection = new SqlConnection(connectionString);
                        await sqlConnection.OpenAsync();
                        connection = sqlConnection;
                        break;

                    case "SQLServer2":
                        var sqlConnection2 = new SqlConnection(connectionString);
                        await sqlConnection2.OpenAsync();
                        connection = sqlConnection2;
                        break;

                    case "MySQL":
                        var mySqlConnection = new MySqlConnection(connectionString);
                        await mySqlConnection.OpenAsync();
                        connection = mySqlConnection;
                        break;

                    case "Oracle":
                        var oracleConnection = new OracleConnection(connectionString);
                        await oracleConnection.OpenAsync();
                        connection = oracleConnection;
                        break;

                    default:
                        throw new ArgumentException($"Unsupported database type: {dbType}");
                }

                return connection;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to open connection for database type: {DbType}", dbType);
                connection?.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Tests a database connection asynchronously.
        /// </summary>
        /// <param name="dbType">Type of the database.</param>
        /// <returns>True if connection successful; otherwise, false.</returns>
        public async Task<bool> TestConnectionAsync(string dbType)
        {
            try
            {
                using var connection = await CreateOpenConnectionAsync(dbType);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Retrieves the connection string for the specified database type.
        /// </summary>
        /// <param name="dbType">Type of the database (e.g., SQLServer, MySQL, Oracle).</param>
        /// <returns>The connection string.</returns>
        private string GetConnectionString(string dbType)
        {
            return dbType switch
            {//temp implementation to be changed
                "SQLServer" => _configuration.GetConnectionString("SQLServerWindowsAuth1"),
                "SQLServer2" => _configuration.GetConnectionString("SQLServerWindowsAuth2"),
                "MySQL" => _configuration.GetConnectionString("MySQL"),
                "Oracle" => _configuration.GetConnectionString("Oracle"),
                _ => throw new ArgumentException($"Unsupported database type: {dbType}")
            };
        }
    }
}