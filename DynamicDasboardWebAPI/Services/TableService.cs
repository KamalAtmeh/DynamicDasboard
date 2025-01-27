using DynamicDashboardCommon.Models;
using DynamicDasboardWebAPI.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DynamicDasboardWebAPI.Services
{
    /// <summary>
    /// Service class for managing table-related operations.
    /// </summary>
    public class TableService
    {
        private readonly TableRepository _repository;

        /// <summary>
        /// Initializes a new instance of the <see cref="TableService"/> class.
        /// </summary>
        /// <param name="repository">The repository instance for table operations.</param>
        public TableService(TableRepository repository)
        {
            _repository = repository;
        }

        /// <summary>
        /// Retrieves tables for a specific database.
        /// </summary>
        /// <param name="databaseId">The ID of the database.</param>
        /// <returns>A collection of tables.</returns>
        public async Task<IEnumerable<Table>> GetTablesByDatabaseIdAsync(int databaseId)
        {
            return await _repository.GetTablesByDatabaseIdAsync(databaseId);
        }

        /// <summary>
        /// Adds a new table.
        /// </summary>
        /// <param name="table">The table to add.</param>
        /// <returns>The ID of the newly added table.</returns>
        /// <exception cref="ArgumentException">Thrown when the table name is null or whitespace.</exception>
        public async Task<int> AddTableAsync(Table table)
        {
            if (string.IsNullOrWhiteSpace(table.DBTableName))
                throw new ArgumentException("Table name is required.");

            return await _repository.AddTableAsync(table);
        }

        /// <summary>
        /// Updates an existing table.
        /// </summary>
        /// <param name="table">The table to update.</param>
        /// <returns>The number of affected rows.</returns>
        /// <exception cref="ArgumentException">Thrown when the table name is null or whitespace.</exception>
        public async Task<int> UpdateTableAsync(Table table)
        {
            if (string.IsNullOrWhiteSpace(table.DBTableName))
                throw new ArgumentException("Table name is required.");

            return await _repository.UpdateTableAsync(table);
        }

        /// <summary>
        /// Deletes a table.
        /// </summary>
        /// <param name="tableId">The ID of the table to delete.</param>
        /// <returns>The number of affected rows.</returns>
        public async Task<int> DeleteTableAsync(int tableId)
        {
            return await _repository.DeleteTableAsync(tableId);
        }
    }
}