using DynamicDashboardCommon.Models;
using DynamicDasboardWebAPI.Repositories;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DynamicDasboardWebAPI.Services
{
    /// <summary>
    /// Service class for handling database operations.
    /// Provides methods to add, update, delete, and retrieve databases.
    /// </summary>
    public class DatabaseService : IDatabaseService
    {
        private readonly DatabaseRepository _repository;

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseService"/> class.
        /// </summary>
        /// <param name="repository">The repository to interact with the database.</param>
        public DatabaseService(DatabaseRepository repository)
        {
            _repository = repository;
        }

        /// <summary>
        /// Retrieves all databases asynchronously.
        /// </summary>
        /// <returns>A collection of databases.</returns>
        public async Task<IEnumerable<Database>> GetAllDatabasesAsync()
        {
            return await _repository.GetAllDatabasesAsync();
        }

        /// <summary>
        /// Adds a new database asynchronously.
        /// </summary>
        /// <param name="database">The database to add.</param>
        /// <returns>The ID of the added database.</returns>
        /// <exception cref="ArgumentException">Thrown when the database name or connection string is invalid.</exception>
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

        /// <summary>
        /// Updates an existing database asynchronously.
        /// </summary>
        /// <param name="database">The database to update.</param>
        /// <returns>The number of affected rows.</returns>
        /// <exception cref="ArgumentException">Thrown when the database name or connection string is invalid.</exception>
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

        /// <summary>
        /// Deletes a database asynchronously.
        /// </summary>
        /// <param name="databaseId">The ID of the database to delete.</param>
        /// <returns>The number of affected rows.</returns>
        public async Task<int> DeleteDatabaseAsync(int databaseId)
        {
            return await _repository.DeleteDatabaseAsync(databaseId);
        }

        /// <summary>
        /// Tests the connection to a database asynchronously.
        /// </summary>
        /// <param name="database">The database to test.</param>
        /// <returns>True if the connection is successful; otherwise, false.</returns>
        public async Task<bool> TestConnectionAsync(Database database)
        {
            return await _repository.TestConnectionAsync(database);
        }

        /// <summary>
        /// Retrieves metadata for a database asynchronously.
        /// </summary>
        /// <param name="databaseID">The ID of the database.</param>
        /// <returns>True if metadata retrieval is successful; otherwise, false.</returns>
        public bool GetDatabaseMetadataAsync(int databaseID)
        {
            return false;
        }
    }
}