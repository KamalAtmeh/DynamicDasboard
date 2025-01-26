using Dapper;
using DynamicDashboardCommon.Models;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace DynamicDasboardWebAPI.Repositories
{
    public class TableRepository
    {
        private readonly IDbConnection _connection;

        public TableRepository(IDbConnection connection)
        {
            _connection = connection;
        }

        // Fetch tables for a specific database
        public async Task<IEnumerable<Table>> GetTablesByDatabaseIdAsync(int databaseId)
        {
            string query = "SELECT * FROM Tables WHERE DatabaseID = @DatabaseID";
            return await _connection.QueryAsync<Table>(query, new { DatabaseID = databaseId });
        }

        // Add a new table
        public async Task<int> AddTableAsync(Table table)
        {
            string query = @"
                INSERT INTO Tables (DatabaseID, DBTableName, AdminTableName, AdminDescription)
                VALUES (@DatabaseID, @DBTableName, @AdminTableName, @AdminDescription)";
            return await _connection.ExecuteAsync(query, table);
        }

        // Update an existing table
        public async Task<int> UpdateTableAsync(Table table)
        {
            string query = @"
                UPDATE Tables
                SET DBTableName = @DBTableName, AdminTableName = @AdminTableName, AdminDescription = @AdminDescription
                WHERE TableID = @TableID";
            return await _connection.ExecuteAsync(query, table);
        }

        // Delete a table
        public async Task<int> DeleteTableAsync(int tableId)
        {
            string query = "DELETE FROM Tables WHERE TableID = @TableID";
            return await _connection.ExecuteAsync(query, new { TableID = tableId });
        }
    }
}