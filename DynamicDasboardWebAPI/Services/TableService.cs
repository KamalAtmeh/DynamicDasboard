using DynamicDashboardCommon.Models;
using DynamicDasboardWebAPI.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DynamicDasboardWebAPI.Services
{
    public class TableService
    {
        private readonly TableRepository _repository;

        public TableService(TableRepository repository)
        {
            _repository = repository;
        }

        // Get tables for a specific database
        public async Task<IEnumerable<Table>> GetTablesByDatabaseIdAsync(int databaseId)
        {
            return await _repository.GetTablesByDatabaseIdAsync(databaseId);
        }

        // Add a new table
        public async Task<int> AddTableAsync(Table table)
        {
            if (string.IsNullOrWhiteSpace(table.DBTableName))
                throw new ArgumentException("Table name is required.");

            return await _repository.AddTableAsync(table);
        }

        // Update an existing table
        public async Task<int> UpdateTableAsync(Table table)
        {
            if (string.IsNullOrWhiteSpace(table.DBTableName))
                throw new ArgumentException("Table name is required.");

            return await _repository.UpdateTableAsync(table);
        }

        // Delete a table
        public async Task<int> DeleteTableAsync(int tableId)
        {
            return await _repository.DeleteTableAsync(tableId);
        }
    }
}