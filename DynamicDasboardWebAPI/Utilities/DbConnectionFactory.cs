using System;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using MySql.Data.MySqlClient;
using Oracle.ManagedDataAccess.Client;
using Microsoft.Extensions.Configuration;

namespace DynamicDasboardWebAPI.Utilities
{
    public class DbConnectionFactory
    {
        private readonly IConfiguration _configuration;

        public DbConnectionFactory(IConfiguration configuration)
        {
            _configuration = configuration;
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
                _ => throw new ArgumentException($"Unsupported database type: {dbType}")
            };
        }

        /// <summary>
        /// Opens a database connection asynchronously based on the specified database type.
        /// </summary>
        /// <param name="dbType">Type of the database (e.g., SQLServer, MySQL, Oracle).</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task OpenConnectionAsync(string dbType)
        {
            try
            {
                string connectionString = GetConnectionString(dbType);

                if (string.IsNullOrWhiteSpace(connectionString))
                    throw new ArgumentException("Connection string is missing or invalid.");

                switch (dbType)
                {
                    case "SQLServer":
                        var sqlConnection = new SqlConnection(connectionString);
                        await sqlConnection.OpenAsync();
                        break;

                    case "MySQL":
                        var mySqlConnection = new MySqlConnection(connectionString);
                        await mySqlConnection.OpenAsync();
                        break;

                    case "Oracle":
                        var oracleConnection = new OracleConnection(connectionString);
                        await oracleConnection.OpenAsync();
                        break;

                    default:
                        throw new ArgumentException($"Unsupported database type: {dbType}");
                }
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Failed to open connection for database type: {DbType}", dbType);
                throw;
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
            {
                "SQLServer" => _configuration.GetConnectionString("SQLServerWindowsAuth"),
                "MySQL" => _configuration.GetConnectionString("MySQL"),
                "Oracle" => _configuration.GetConnectionString("Oracle"),
                _ => throw new ArgumentException($"Unsupported database type: {dbType}")
            };
        }
    }
}