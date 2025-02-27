using DynamicDashboardCommon.Models;
using DynamicDasboardWebAPI.Repositories;
using Microsoft.Data.SqlClient;
using MySql.Data.MySqlClient;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DynamicDasboardWebAPI.Services
{
    public class DatabaseService : IDatabaseService
    {
        private readonly DatabaseRepository _repository;

        public DatabaseService(DatabaseRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<Database>> GetAllDatabasesAsync()
        {
            return await _repository.GetAllDatabasesAsync();
        }

        public async Task<int> AddDatabaseAsync(Database database)
        {
            if (string.IsNullOrWhiteSpace(database.Name))
                throw new ArgumentException("Database name cannot be empty.");

            if (string.IsNullOrWhiteSpace(database.ConnectionString))
                throw new ArgumentException("Connection string cannot be empty.");

            if (!await _repository.TestConnectionAsync(database))
                throw new ArgumentException("Invalid connection string.");

            return await _repository.AddDatabaseAsync(database);
        }

        public async Task<int> UpdateDatabaseAsync(Database database)
        {
            if (string.IsNullOrWhiteSpace(database.Name))
                throw new ArgumentException("Database name cannot be empty.");

            if (string.IsNullOrWhiteSpace(database.ConnectionString))
                throw new ArgumentException("Connection string cannot be empty.");

            if (!await _repository.TestConnectionAsync(database))
                throw new ArgumentException("Invalid connection string.");

            return await _repository.UpdateDatabaseAsync(database);
        }

        public async Task<int> DeleteDatabaseAsync(int databaseId)
        {
            return await _repository.DeleteDatabaseAsync(databaseId);
        }

        public async Task<bool> TestConnectionAsync(Database database)
        {
            return await _repository.TestConnectionAsync(database);
        }

        public async Task<Database> GetDatabaseByIdAsync(int databaseId)
        {
            if (databaseId <= 0)
                throw new ArgumentException("Database ID must be greater than zero.");

            return await _repository.GetDatabaseByIdAsync(databaseId);
        }

        public async Task<ConnectionTestResult> TestConnectionAsync(ConnectionTestRequest request)
        {
            try
            {
                // Create a temporary Database object from the request
                var database = new Database
                {
                    TypeID = GetDatabaseTypeId(request.DbType),
                    ConnectionString = BuildConnectionString(request)
                };

                var success = await _repository.TestConnectionAsync(database);

                return new ConnectionTestResult
                {
                    Success = success,
                    Message = success ? "Connection successful!" : "Connection failed."
                };
            }
            catch (Exception ex)
            {
                return new ConnectionTestResult
                {
                    Success = false,
                    Message = $"Connection failed: {ex.Message}",
                    ErrorDetails = ex.ToString()
                };
            }
        }

        public List<string> GetSupportedDatabaseTypes()
        {
            return new List<string> { "SQLServer", "MySQL", "Oracle" };
        }

        public bool GetDatabaseMetadataAsync(int databaseID)
        {
            return false;
        }

        private int GetDatabaseTypeId(string dbType)
        {
            return dbType.ToLower() switch
            {
                "sqlserver" => 1,
                "mysql" => 2,
                "oracle" => 3,
                _ => throw new NotSupportedException($"Database type '{dbType}' is not supported.")
            };
        }

        private string BuildConnectionString(ConnectionTestRequest request)
        {
            switch (request.DbType.ToLower())
            {
                case "sqlserver":
                    return BuildSqlServerConnectionString(request);
                case "mysql":
                    return BuildMySqlConnectionString(request);
                case "oracle":
                    return BuildOracleConnectionString(request);
                default:
                    throw new NotSupportedException($"Database type '{request.DbType}' is not supported.");
            }
        }

        private string BuildSqlServerConnectionString(ConnectionTestRequest request)
        {
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = request.Server,
                InitialCatalog = request.Database
            };

            if (request.AuthType.ToLower() == "windows")
            {
                builder.IntegratedSecurity = true;
            }
            else
            {
                builder.UserID = request.Username;
                builder.Password = request.Password;
            }

            // Add optional timeout
            builder.ConnectTimeout = 30;

            return builder.ConnectionString;
        }

        private string BuildMySqlConnectionString(ConnectionTestRequest request)
        {
            var builder = new MySqlConnectionStringBuilder
            {
                Server = request.Server,
                Database = request.Database,
                UserID = request.Username,
                Password = request.Password,
                ConnectionTimeout = 30
            };

            return builder.ConnectionString;
        }

        private string BuildOracleConnectionString(ConnectionTestRequest request)
        {
            var builder = new OracleConnectionStringBuilder
            {
                DataSource = request.Server,
                UserID = request.Username,
                Password = request.Password,
                ConnectionTimeout = 30
            };

            return builder.ConnectionString;
        }
    }
}