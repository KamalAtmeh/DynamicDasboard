using DynamicDashboardCommon.Models;
using DynamicDasboardWebAPI.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DynamicDasboardWebAPI.Services
{
    /// <summary>
    /// Service class for managing columns in the database.
    /// Provides methods to get, add, update, and delete columns.
    /// </summary>
    public class ColumnService
    {
        private readonly ColumnRepository _repository;

        /// <summary>
        /// Initializes a new instance of the <see cref="ColumnService"/> class.
        /// </summary>
        /// <param name="repository">The repository to interact with the database.</param>
        public ColumnService(ColumnRepository repository)
        {
            _repository = repository;
        }

        /// <summary>
        /// Gets the columns for a specific table.
        /// </summary>
        /// <param name="tableId">The ID of the table.</param>
        /// <returns>A list of columns for the specified table.</returns>
        public async Task<IEnumerable<Column>> GetColumnsByTableIdAsync(int tableId)
        {
            return await _repository.GetColumnsByTableIdAsync(tableId);
        }

        /// <summary>
        /// Adds a new column to the database.
        /// </summary>
        /// <param name="column">The column to add.</param>
        /// <returns>The ID of the newly added column.</returns>
        /// <exception cref="ArgumentException">Thrown when the column name is null or whitespace.</exception>
        public async Task<int> AddColumnAsync(Column column)
        {
            if (string.IsNullOrWhiteSpace(column.DBColumnName))
                throw new ArgumentException("Column name is required.");

            return await _repository.AddColumnAsync(column);
        }

        /// <summary>
        /// Updates an existing column in the database.
        /// </summary>
        /// <param name="column">The column to update.</param>
        /// <returns>The number of affected rows.</returns>
        /// <exception cref="ArgumentException">Thrown when the column name is null or whitespace.</exception>
        public async Task<int> UpdateColumnAsync(Column column)
        {
            if (string.IsNullOrWhiteSpace(column.DBColumnName))
                throw new ArgumentException("Column name is required.");

            return await _repository.UpdateColumnAsync(column);
        }

        /// <summary>
        /// Deletes a column from the database.
        /// </summary>
        /// <param name="columnId">The ID of the column to delete.</param>
        /// <returns>The number of affected rows.</returns>
        public async Task<int> DeleteColumnAsync(int columnId)
        {
            return await _repository.DeleteColumnAsync(columnId);
        }
    }
}