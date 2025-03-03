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
    public class DatabaseService
    {
        private readonly DatabaseRepository _repository;
        private readonly DbConnectionFactory _connectionFactory;
        private readonly ILogger<DatabaseService> _logger;
        private readonly ConcurrentDictionary<string, int> _typeIdCache = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseService"/> class.
        /// </summary>
        /// <param name="repository">The repository for database operations.</param>
 
        /// <param name="logger">The logger for service operations.</param>
        public DatabaseService(
            DatabaseRepository repository,
            ILogger<DatabaseService> logger = null)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger;

            //// Load all database types at startup
            //LoadDatabaseTypesAsync().ConfigureAwait(false).GetAwaiter().GetResult();
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
                return 0;

            try
            {

                // Validate required fields
                if (!string.IsNullOrWhiteSpace(database.Name) && !string.IsNullOrWhiteSpace(database.ServerAddress) && !string.IsNullOrEmpty(database.DatabaseName))
                {


                    // Set initial values for new database
                    database.CreatedAt = DateTime.UtcNow;
                    database.IsActive = true;

                    int databaseId = await _repository.AddDatabaseAsync(database);

                    return databaseId;
                }
                else
                {
                    return 0;
                }
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
                return 0;

            try
            {

                if (!string.IsNullOrWhiteSpace(database.Name) && !string.IsNullOrWhiteSpace(database.ServerAddress) && !string.IsNullOrEmpty(database.DatabaseName))
                {     // Validate required fields


                    int result = await _repository.UpdateDatabaseAsync(database);

                    return result;
                }
                else
                {
                    return 0;
                }
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
               

                int result = await _repository.DeleteDatabaseAsync(databaseId);



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

        /// <summary>
        /// Tests a database connection using the provided connection details.
        /// </summary>
        /// <param name="request">The connection test request.</param>
        /// <returns>The connection test result.</returns>
        public async Task<bool> TestConnectionAsync(Database database)
        {

            if(database == null)
            {
                return false;
            }

            try
            {


                if (database.DatabaseID != 0)
                {
                    database = await GetDatabaseByIdAsync(database.DatabaseID);
                }
                else
                {
                    // Convert request to a temporary database object
                    database = new Database
                    {

                        DatabaseID = database.DatabaseID,
                        ServerAddress = database.ServerAddress,
                        DatabaseName = database.DatabaseName,
                        TypeID = database.TypeID,
                        Port = database.Port,
                        Username = database.Username,
                        EncryptedCredentials = database.EncryptedCredentials // Note: In production, this should be encrypted //temp
                    };
                }
                // Test connection
                bool isSuccess = await _repository.TestConnectionAsync(database);
                return isSuccess;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error testing connection with parameters: {Server}/{Database}",
                    database.ServerAddress, database.DatabaseName);
                throw;
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
                return await _repository.GetSupportedDatabaseTypesAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving supported database types");

                // Fallback only if database query fails
                throw;
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

                return true; //temp
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving metadata for database: {DatabaseID}", databaseId);
                throw;
                
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
                var database = await _repository.GetDatabaseByIdAsync(databaseId);

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
                return await _repository.GetSupportedDatabaseTypesAsync();
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
                string typeName = await _repository.GetDatabaseTypeNameAsync(typeId);

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


     
    }
}