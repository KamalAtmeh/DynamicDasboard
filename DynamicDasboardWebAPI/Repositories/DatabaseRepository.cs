using Dapper;
using DynamicDashboardCommon.Models;
using DynamicDasboardWebAPI.Utilities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;

namespace DynamicDasboardWebAPI.Repositories
{
    public class DatabaseRepository
    {
        private readonly IDbConnection _appDbConnection; // Application Database
        private readonly DbConnectionFactory _dynamicDbConnectionFactory; // Dynamic Database
        private readonly ILogger<DatabaseRepository> _logger;

        public DatabaseRepository(
            IDbConnection appDbConnection,
            DbConnectionFactory dynamicDbConnectionFactory,
            ILogger<DatabaseRepository> logger = null)
        {
            _appDbConnection = appDbConnection ?? throw new ArgumentNullException(nameof(appDbConnection));
            _dynamicDbConnectionFactory = dynamicDbConnectionFactory ?? throw new ArgumentNullException(nameof(dynamicDbConnectionFactory));
            _logger = logger;
        }

        // Fetch all databases from the Application Database
        public async Task<IEnumerable<Database>> GetAllDatabasesAsync()
        {
            try
            {
                const string query = "SELECT * FROM Databases";
                return await _appDbConnection.QuerySafeAsync<Database>(query);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving all databases");
                throw;
            }
        }

        // Get a database by ID
        public async Task<Database> GetDatabaseByIdAsync(int databaseId)
        {
            try
            {
                const string query = "SELECT * FROM Databases WHERE DatabaseID = @DatabaseID";
                return await _appDbConnection.QueryFirstOrDefaultSafeAsync<Database>(query, new { DatabaseID = databaseId });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving database by ID: {DatabaseID}", databaseId);
                throw;
            }
        }

        // Get a database by name
        public async Task<Database> GetDatabaseByNameAsync(string databaseName)
        {
            try
            {
                const string query = "SELECT * FROM Databases WHERE Name = @Name";
                return await _appDbConnection.QueryFirstOrDefaultSafeAsync<Database>(query, new { Name = databaseName });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving database by name: {Name}", databaseName);
                throw;
            }
        }

        // Add a new database connection
        public async Task<int> AddDatabaseAsync(Database database)
        {
            try
            {
                if (database == null) throw new ArgumentNullException(nameof(database));

                const string query = @"
                    INSERT INTO Databases 
                    (Name, TypeID, ConnectionString, Description, CreatedBy, DBCreationScript) 
                    VALUES 
                    (@Name, @TypeID, @ConnectionString, @Description, @CreatedBy, @DBCreationScript);
                    SELECT CAST(SCOPE_IDENTITY() as int)";

                return await _appDbConnection.ExecuteScalarSafeAsync<int>(query, database);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error adding database: {Name}", database?.Name);
                throw;
            }
        }

        // Update an existing database connection
        public async Task<int> UpdateDatabaseAsync(Database database)
        {
            try
            {
                if (database == null) throw new ArgumentNullException(nameof(database));

                const string query = @"
                    UPDATE Databases 
                    SET Name = @Name, 
                        TypeID = @TypeID, 
                        ConnectionString = @ConnectionString,
                        Description = @Description,
                        DBCreationScript = @DBCreationScript
                    WHERE DatabaseID = @DatabaseID";

                return await _appDbConnection.ExecuteSafeAsync(query, database);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating database: {DatabaseID}", database?.DatabaseID);
                throw;
            }
        }

        // Delete a database connection
        public async Task<int> DeleteDatabaseAsync(int databaseId)
        {
            try
            {
                const string query = "DELETE FROM Databases WHERE DatabaseID = @DatabaseID";
                return await _appDbConnection.ExecuteSafeAsync(query, new { DatabaseID = databaseId });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error deleting database: {DatabaseID}", databaseId);
                throw;
            }
        }

        // Get database metadata as tables
        public async Task<IEnumerable<Table>> GetDatabaseMetadataAsync(int databaseId)
        {
            try
            {
                const string query = "SELECT * FROM Tables WHERE DatabaseID = @DatabaseID";
                return await _appDbConnection.QuerySafeAsync<Table>(query, new { DatabaseID = databaseId });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving metadata for database: {DatabaseID}", databaseId);
                throw;
            }
        }

        /// <summary>
        /// Get a comprehensive database details by ID
        /// </summary>
        public async Task<Database> GetDatabaseDetailsByIdAsync(int databaseId)
        {
            try
            {
                // Comprehensive query to fetch all database details with type name
                const string query = @"
                    SELECT 
                        d.DatabaseID, 
                        d.Name, 
                        d.TypeID, 
                        d.ServerAddress, 
                        d.DatabaseName, 
                        d.Port, 
                        d.Username, 
                        d.EncryptedCredentials, 
                        d.CreatedAt, 
                        d.CreatedBy, 
                        d.Description, 
                        d.IsActive, 
                        d.LastTransactionDate, 
                        d.LastConnectionStatus, 
                        d.DBCreationScript, 
                        d.ConnectionString,
                        dt.TypeName as DatabaseTypeName
                    FROM Databases d
                    LEFT JOIN DatabaseTypes dt ON d.TypeID = dt.TypeID
                    WHERE d.DatabaseID = @DatabaseID";

                return await _appDbConnection.QueryFirstOrDefaultSafeAsync<Database>(query, new { DatabaseID = databaseId });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving database details by ID: {DatabaseID}", databaseId);
                throw;
            }
        }

        /// <summary>
        /// Get all databases with their type names
        /// </summary>
        public async Task<IEnumerable<Database>> GetAllDatabasesWithTypesAsync()
        {
            try
            {
                const string query = @"
                    SELECT 
                        d.DatabaseID, 
                        d.Name, 
                        d.TypeID, 
                        d.ServerAddress, 
                        d.DatabaseName, 
                        d.Port, 
                        d.Username, 
                        d.EncryptedCredentials, 
                        d.CreatedAt, 
                        d.CreatedBy, 
                        d.Description, 
                        d.IsActive, 
                        d.LastTransactionDate, 
                        d.LastConnectionStatus, 
                        d.DBCreationScript, 
                        d.ConnectionString,
                        dt.TypeName as DatabaseTypeName
                    FROM Databases d
                    LEFT JOIN DatabaseTypes dt ON d.TypeID = dt.TypeID";

                return await _appDbConnection.QuerySafeAsync<Database>(query);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving databases with types");
                throw;
            }
        }

        /// <summary>
        /// Get database type name by ID
        /// </summary>
        public async Task<string> GetDatabaseTypeNameAsync(int typeId)
        {
            try
            {
                const string query = "SELECT TypeName FROM DatabaseTypes WHERE TypeID = @TypeID";
                return await _appDbConnection.QueryFirstOrDefaultSafeAsync<string>(query, new { TypeID = typeId });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving database type name for ID: {TypeID}", typeId);

                // Fallback to hardcoded type names
                return typeId switch
                {
                    1 => "SQLServer",
                    2 => "MySQL",
                    3 => "Oracle",
                    4 => "SQLServer2",
                    _ => $"CustomType_{typeId}"
                };
            }
        }

        /// <summary>
        /// Get all database types
        /// </summary>
        public async Task<IEnumerable<DatabaseType>> GetAllDatabaseTypesAsync()
        {
            try
            {
                const string query = "SELECT TypeID, TypeName FROM DatabaseTypes";
                return await _appDbConnection.QueryAsync<DatabaseType>(query);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving database types");

                // Fallback to hardcoded types
                return null;
            }
        }

        /// <summary>
        /// Tests the connection to a database
        /// </summary>
        public async Task<bool> TestConnectionAsync(int databaseId)
        {
            try
            {
                return await _dynamicDbConnectionFactory.TestConnectionAsync(databaseId.ToString());
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error testing connection to database with ID: {DatabaseId}", databaseId);
                return false;
            }
        }

        /// <summary>
        /// Tests the connection using connection details without saving to the database
        /// </summary>
        public async Task<bool> TestConnectionAsync(Database database)
        {
            try
            {
                return await _dynamicDbConnectionFactory.TestConnectionAsync(database);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error testing connection to database: {Name}", database?.Name);
                return false;
            }
        }

        /// <summary>
        /// Executes a SQL query against a specific database
        /// </summary>
        public async Task<IEnumerable<T>> ExecuteQueryAsync<T>(int databaseId, string query, object parameters = null)
        {
            try
            {
                using var connection = await _dynamicDbConnectionFactory.CreateOpenConnectionAsync(databaseId.ToString());
                return await connection.QueryAsync<T>(query, parameters);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error executing query on database {DatabaseId}: {Query}", databaseId, query);
                throw;
            }
        }

        /// <summary>
        /// Gets a database type name by ID from the DatabaseTypes table
        /// </summary>
        public async Task<string> GetDatabaseTypeNameByIdAsync(int typeId)
        {
            try
            {
                const string query = "SELECT TypeName FROM DatabaseTypes WHERE TypeID = @TypeID";
                return await _appDbConnection.QueryFirstOrDefaultSafeAsync<string>(query, new { TypeID = typeId });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving database type name for ID: {TypeId}", typeId);

                // Fallback to hardcoded values if database lookup fails
                return typeId switch
                {
                    1 => "SQLServer",
                    2 => "MySQL",
                    3 => "Oracle",
                    4 => "SQLServer2",
                    _ => $"Unknown_{typeId}"
                };
            }
        }

        /// <summary>
        /// Updates the LastConnectionStatus and LastTransactionDate for a database
        /// </summary>
        public async Task UpdateConnectionStatusAsync(int databaseId, bool status)
        {
            try
            {
                const string query = @"
                UPDATE Databases 
                SET LastConnectionStatus = @Status, LastTransactionDate = GETDATE()
                WHERE DatabaseID = @DatabaseID";

                await _appDbConnection.ExecuteSafeAsync(query, new
                {
                    DatabaseID = databaseId,
                    Status = status
                });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating connection status for database: {DatabaseId}", databaseId);
            }
        }


        // Helper method to convert TypeID to database type name
        private string GetDatabaseTypeName(int typeId)
        {
            return typeId switch
            {
                1 => "SQLServer",
                2 => "MySQL",
                3 => "Oracle",
                4 => "SQLServer2",
                _ => throw new ArgumentException($"Invalid database type: {typeId}")
            };
        }




    }
}