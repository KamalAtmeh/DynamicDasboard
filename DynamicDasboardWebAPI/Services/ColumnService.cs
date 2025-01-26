using DynamicDashboardCommon.Models;
using DynamicDasboardWebAPI.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DynamicDasboardWebAPI.Services
{
    public class ColumnService
    {
        private readonly ColumnRepository _repository;

        public ColumnService(ColumnRepository repository)
        {
            _repository = repository;
        }

        // Get columns for a specific table
        public async Task<IEnumerable<Column>> GetColumnsByTableIdAsync(int tableId)
        {
            return await _repository.GetColumnsByTableIdAsync(tableId);
        }

        // Add a new column
        public async Task<int> AddColumnAsync(Column column)
        {
            if (string.IsNullOrWhiteSpace(column.DBColumnName))
                throw new ArgumentException("Column name is required.");

            return await _repository.AddColumnAsync(column);
        }

        // Update an existing column
        public async Task<int> UpdateColumnAsync(Column column)
        {
            if (string.IsNullOrWhiteSpace(column.DBColumnName))
                throw new ArgumentException("Column name is required.");

            return await _repository.UpdateColumnAsync(column);
        }

        // Delete a column
        public async Task<int> DeleteColumnAsync(int columnId)
        {
            return await _repository.DeleteColumnAsync(columnId);
        }
    }
}