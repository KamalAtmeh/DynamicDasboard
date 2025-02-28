using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Data;
using Dapper;
using Microsoft.Extensions.Logging;
using DynamicDashboardCommon.Models;
using DynamicDasboardWebAPI.Repositories;
using DynamicDasboardWebAPI.Utilities;

namespace DynamicDasboardWebAPI.Services
{
    /// <summary>
    /// Service for managing database operations including connections, metadata, and queries.
    /// </summary>
    public class DatabaseService : IDatabaseService
    {
        private readonly DatabaseRepository _repository;
        private readonly DbConnectionFactory _connectionFactory;
        private readonly ILogger<DatabaseService> _logger;
        private readonly ConcurrentDictionary<string, int> _typeIdCache = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseService"/> class.
        /// </summary>
        /// <param name="repository">The repository for database operations.</param>
        /// <param name="connectionFactory">The factory for database connections.</param>
        /// <param name="logger">The logger for service operations.</param>
        public DatabaseService(
            DatabaseRepository repository,
            DbConnectionFactory connectionFactory,
            ILogger<DatabaseService> logger = null)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _logger = logger;

            // Load all database types at startup
            LoadDatabaseTypesAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Retrieves all databases from the system.
        /// </summary>
        /// <returns>A collection of all databases.</returns>
        public async Task<IEnumerable<Database>> GetAllDatabasesAsync()
        {
            try
            {
                _logger?.LogInformation("Retrieving all databases");
                var databases = await _repository.GetAllDatabasesAsync();

                // Enrich with type names if needed
                foreach (var db in databases)
                {
                    if (string.IsNullOrEmpty(db.DatabaseTypeName))
                    {
                        db.DatabaseTypeName = await GetDatabaseTypeNameAsync(db.TypeID);
                    }
                }

                return databases;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving all databases");
                throw;
            }
        }

        /// <summary>
        /// Adds a new database to the system.
        /// </summary>
        /// <param name="database">The database to add.</param>
        /// <returns>The ID of the newly added database.</returns>
        public async Task<int> AddDatabaseAsync(Database database)
        {
            if (database == null)
                throw new ArgumentNullException(nameof(database));

            try
            {
                _logger?.LogInformation("Adding new database: {Name}", database.Name);

                // Validate required fields
                if (string.IsNullOrWhiteSpace(database.Name))
                    throw new ArgumentException("Database name cannot be empty");

                if (string.IsNullOrWhiteSpace(database.ServerAddress))
                    throw new ArgumentException("Server address cannot be empty");

                if (string.IsNullOrWhiteSpace(database.DatabaseName))
                    throw new ArgumentException("Database name cannot be empty");

                // Build connection string if not provided
                if (string.IsNullOrWhiteSpace(database.ConnectionString))
                {
                    database.ConnectionString = _connectionFactory.BuildConnectionString(database);
                }

                // Test connection before adding
                bool connectionSuccess = await _connectionFactory.TestConnectionAsync(database);
                if (!connectionSuccess)
                {
                    throw new InvalidOperationException("Connection test failed. Please check connection details.");
                }

                // Set initial values for new database
                database.CreatedAt = DateTime.UtcNow;
                database.LastConnectionStatus = true;
                database.LastTransactionDate = DateTime.UtcNow;
                database.IsActive = true;

                int databaseId = await _repository.AddDatabaseAsync(database);

                // Invalidate cache after adding
                _connectionFactory.ClearCache();

                return databaseId;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error adding database: {Name}", database.Name);
                throw;
            }
        }

        /// <summary>
        /// Updates an existing database in the system.
        /// </summary>
        /// <param name="database">The database to update.</param>
        /// <returns>The number of affected rows.</returns>
        public async Task<int> UpdateDatabaseAsync(Database database)
        {
            if (database == null)
                throw new ArgumentNullException(nameof(database));

            try
            {
                _logger?.LogInformation("Updating database: {DatabaseID} - {Name}", database.DatabaseID, database.Name);

                // Validate required fields
                if (string.IsNullOrWhiteSpace(database.Name))
                    throw new ArgumentException("Database name cannot be empty");

                if (string.IsNullOrWhiteSpace(database.ServerAddress))
                    throw new ArgumentException("Server address cannot be empty");

                if (string.IsNullOrWhiteSpace(database.DatabaseName))
                    throw new ArgumentException("Database name cannot be empty");

                // Build connection string if not provided
                if (string.IsNullOrWhiteSpace(database.ConnectionString))
                {
                    database.ConnectionString = _connectionFactory.BuildConnectionString(database);
                }

                // Test connection before updating
                bool connectionSuccess = await _connectionFactory.TestConnectionAsync(database);
                database.LastConnectionStatus = connectionSuccess;
                database.LastTransactionDate = DateTime.UtcNow;

                int result = await _repository.UpdateDatabaseAsync(database);

                // Invalidate cache after updating
                _connectionFactory.ClearCache();

                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating database: {DatabaseID}", database.DatabaseID);
                throw;
            }
        }

        /// <summary>
        /// Deletes a database from the system.
        /// </summary>
        /// <param name="databaseId">The ID of the database to delete.</param>
        /// <returns>The number of affected rows.</returns>
        public async Task<int> DeleteDatabaseAsync(int databaseId)
        {
            try
            {
                _logger?.LogInformation("Deleting database: {DatabaseID}", databaseId);

                int result = await _repository.DeleteDatabaseAsync(databaseId);

                // Invalidate cache after deleting
                _connectionFactory.ClearCache();

                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error deleting database: {DatabaseID}", databaseId);
                throw;
            }
        }

        /// <summary>
        /// Tests the connection to a database using stored connection details.
        /// </summary>
        /// <param name="databaseId">The ID of the database to test.</param>
        /// <returns>True if the connection was successful; otherwise, false.</returns>
        public async Task<bool> TestConnectionAsync(int databaseId)
        {
            try
            {
                _logger?.LogInformation("Testing connection to database: {DatabaseID}", databaseId);

                // Get database record
                var database = await _repository.GetDatabaseByIdAsync(databaseId);
                if (database == null)
                {
                    _logger?.LogWarning("Database not found: {DatabaseID}", databaseId);
                    return false;
                }

                // Test connection
                bool success = await _connectionFactory.TestConnectionAsync(databaseId.ToString());

                // Update last connection status
                await _repository.UpdateConnectionStatusAsync(databaseId, success);

                return success;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error testing connection to database: {DatabaseID}", databaseId);

                // Update connection status to failed
                await _repository.UpdateConnectionStatusAsync(databaseId, false);

                return false;
            }
        }

        /// <summary>
        /// Tests a database connection using the provided connection details.
        /// </summary>
        /// <param name="request">The connection test request.</param>
        /// <returns>The connection test result.</returns>
        public async Task<ConnectionTestResult> TestConnectionAsync(ConnectionTestRequest request)
        {

            var database = new Database();
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            try
            {
                _logger?.LogInformation("Testing connection using request parameters: {Server}/{Database}",
                    request.Server, request.Database);
                if (request.DatabaseId != 0)
                {
                    database = await GetDatabaseByIdAsync(request.DatabaseId);
                }
                else
                {
                    // Convert request to a temporary database object
                    database = new Database
                    {

                        DatabaseID = request.DatabaseId,
                        ServerAddress = request.Server,
                        DatabaseName = request.Database,
                        Port = null, // Let the connection string builder use defaults
                        Username = request.AuthType?.ToLowerInvariant() == "windows" ? null : request.Username,
                        EncryptedCredentials = request.Password // Note: In production, this should be encrypted
                    };
                }
                // Test connection
                bool success = await _connectionFactory.TestConnectionAsync(database);

                return new ConnectionTestResult
                {
                    Success = success,
                    Message = success ? "Connection successful!" : "Connection failed. Check your connection details."
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error testing connection with parameters: {Server}/{Database}",
                    request.Server, request.Database);

                return new ConnectionTestResult
                {
                    Success = false,
                    Message = $"Connection failed: {ex.Message}",
                    ErrorDetails = ex.ToString()
                };
            }
        }

        /// <summary>
        /// Gets a list of supported database types.
        /// </summary>
        /// <returns>A list of supported database types.</returns>
        public async Task<List<DatabaseType>> GetSupportedDatabaseTypesAsync()
        {
            try
            {
                _logger?.LogInformation("Retrieving supported database types");

                // Get types from repository
                var databaseTypes = await _repository.GetAllDatabaseTypesAsync();
                return databaseTypes.ToList();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving supported database types");

                // Fallback only if database query fails
                return null;
            }
        }

        /// <summary>
        /// Gets database metadata for the specified database.
        /// </summary>
        /// <param name="databaseId">The ID of the database.</param>
        /// <returns>True if metadata was retrieved successfully; otherwise, false.</returns>
        public async Task<bool> GetDatabaseMetadataAsync(int databaseId)
        {
            try
            {
                _logger?.LogInformation("Retrieving metadata for database: {DatabaseID}", databaseId);

                var tables = await _repository.GetDatabaseMetadataAsync(databaseId);
                return tables != null && tables.Any();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving metadata for database: {DatabaseID}", databaseId);
                return false;
            }
        }

        /// <summary>
        /// Gets a database by ID.
        /// </summary>
        /// <param name="databaseId">The ID of the database to retrieve.</param>
        /// <returns>The database with the specified ID, or null if not found.</returns>
        public async Task<Database> GetDatabaseByIdAsync(int databaseId)
        {
            try
            {
                _logger?.LogInformation("Retrieving database by ID: {DatabaseID}", databaseId);

                var database = await _repository.GetDatabaseByIdAsync(databaseId);

                // Enrich with type name if needed
                if (database != null && string.IsNullOrEmpty(database.DatabaseTypeName))
                {
                    database.DatabaseTypeName = await GetDatabaseTypeNameAsync(database.TypeID);
                }

                return database;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving database by ID: {DatabaseID}", databaseId);
                throw;
            }
        }

        /// <summary>
        /// Gets all database types.
        /// </summary>
        /// <returns>A collection of database types.</returns>
        public async Task<IEnumerable<DatabaseType>> GetAllDatabaseTypesAsync()
        {
            try
            {
                _logger?.LogInformation("Retrieving all database types");
                return await _repository.GetAllDatabaseTypesAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving database types");
                throw;
            }
        }

        /// <summary>
        /// Gets the database type name for the specified ID.
        /// </summary>
        /// <param name="typeId">The type ID.</param>
        /// <returns>The database type name.</returns>
        public async Task<string> GetDatabaseTypeNameAsync(int typeId)
        {
            try
            {
                // Check cache first (inverted lookup)
                foreach (var kvp in _typeIdCache)
                {
                    if (kvp.Value == typeId)
                    {
                        return kvp.Key;
                    }
                }

                // If not in cache, get from repository
                string typeName = await _repository.GetDatabaseTypeNameByIdAsync(typeId);

                // If found, add to cache
                if (!string.IsNullOrEmpty(typeName))
                {
                    _typeIdCache.TryAdd(typeName.ToLowerInvariant(), typeId);
                }

                return typeName;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving database type name for ID: {TypeID}", typeId);

                // Fallback
                return string.Empty;
            }
        }

        /// <summary>
        /// Executes a query on a specific database.
        /// </summary>
        /// <typeparam name="T">The type to map results to.</typeparam>
        /// <param name="databaseId">The ID of the database to query.</param>
        /// <param name="query">The SQL query to execute.</param>
        /// <param name="parameters">The query parameters.</param>
        /// <returns>The query results.</returns>
        public async Task<IEnumerable<T>> ExecuteQueryAsync<T>(int databaseId, string query, object parameters = null)
        {
            try
            {
                _logger?.LogInformation("Executing query on database {DatabaseID}: {Query}", databaseId, query);

                var results = await _repository.ExecuteQueryAsync<T>(databaseId, query, parameters);

                // Update last transaction date and connection status
                await _repository.UpdateConnectionStatusAsync(databaseId, true);

                return results;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error executing query on database {DatabaseID}: {Query}", databaseId, query);

                // Update connection status to failed
                await _repository.UpdateConnectionStatusAsync(databaseId, false);

                throw;
            }
        }

        /// <summary>
        /// Gets the database type ID for the specified type name.
        /// </summary>
        /// <param name="dbType">The database type name.</param>
        /// <returns>The type ID.</returns>
        private async Task<int> GetDatabaseTypeIdAsync(string dbType)
        {
            if (string.IsNullOrWhiteSpace(dbType))
                throw new ArgumentException("Database type cannot be empty", nameof(dbType));

            // Normalize type name
            string normalizedType = dbType.ToLowerInvariant();

            // Check cache first
            if (_typeIdCache.TryGetValue(normalizedType, out int cachedId))
                return cachedId;

            try
            {
                // Get from repository
                var types = await _repository.GetAllDatabaseTypesAsync();
                var match = types.FirstOrDefault(t =>
                    string.Equals(t.TypeName, dbType, StringComparison.OrdinalIgnoreCase));

                if (match != null)
                {
                    // Add to cache and return
                    _typeIdCache.TryAdd(normalizedType, match.TypeID);
                    return match.TypeID;
                }
                return 0;

            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting type ID for database type: {DbType}", dbType);
                return 0;
            }
        }

        ///// <summary>
        ///// Gets the fallback type ID for the specified type name.
        ///// </summary>
        ///// <param name="normalizedType">The normalized type name.</param>
        ///// <returns>The type ID.</returns>
        //private int GetFallbackTypeId(string normalizedType)
        //{
        //    return normalizedType switch
        //    {
        //        "sqlserver" => 1,
        //        "mysql" => 2,
        //        "oracle" => 3,
        //        "sqlserver2" => 4,
        //        _ => throw new ArgumentException($"Unsupported database type: {normalizedType}")
        //    };
        //}

        /// <summary>
        /// Loads all database types into the cache.
        /// </summary>
        private async Task LoadDatabaseTypesAsync()
        {
            try
            {
                _logger?.LogInformation("Loading database types into cache");

                var types = await _repository.GetAllDatabaseTypesAsync();

                foreach (var type in types)
                {
                    _typeIdCache.TryAdd(type.TypeName.ToLowerInvariant(), type.TypeID);
                }

                _logger?.LogInformation("Loaded {Count} database types into cache", _typeIdCache.Count);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to load database types into cache");

                // Add fallback types to cache
                _typeIdCache.TryAdd("sqlserver", 1);
                _typeIdCache.TryAdd("mysql", 2);
                _typeIdCache.TryAdd("oracle", 3);
            }
        }
    }
}