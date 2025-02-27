using Dapper;
using DynamicDashboardCommon.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using DynamicDasboardWebAPI.Utilities;

namespace DynamicDasboardWebAPI.Repositories
{
    public class TableRepository
    {
        private readonly IDbConnection _connection;
        private readonly ILogger<TableRepository> _logger;

        public TableRepository(IDbConnection connection, ILogger<TableRepository> logger = null)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _logger = logger;
        }

        // Fetch tables for a specific database
        public async Task<IEnumerable<Table>> GetTablesByDatabaseIdAsync(int databaseId)
        {
            try
            {
                const string query = "SELECT * FROM Tables WHERE DatabaseID = @DatabaseID";
                return await _connection.QuerySafeAsync<Table>(query, new { DatabaseID = databaseId });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving tables for database: {DatabaseID}", databaseId);
                throw;
            }
        }

        // Get a specific table by ID
        public async Task<Table> GetTableByIdAsync(int tableId)
        {
            try
            {
                const string query = "SELECT * FROM Tables WHERE TableID = @TableID";
                return await _connection.QueryFirstOrDefaultSafeAsync<Table>(query, new { TableID = tableId });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving table by ID: {TableID}", tableId);
                throw;
            }
        }

        // Add a new table
        public async Task<int> AddTableAsync(Table table)
        {
            try
            {
                if (table == null) throw new ArgumentNullException(nameof(table));

                const string query = @"
                    INSERT INTO Tables (DatabaseID, DBTableName, AdminTableName, AdminDescription)
                    VALUES (@DatabaseID, @DBTableName, @AdminTableName, @AdminDescription);
                    SELECT CAST(SCOPE_IDENTITY() as int)";

                return await _connection.ExecuteScalarSafeAsync<int>(query, table);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error adding table: {DBTableName}", table?.DBTableName);
                throw;
            }
        }

        // Update an existing table
        public async Task<int> UpdateTableAsync(Table table)
        {
            try
            {
                if (table == null) throw new ArgumentNullException(nameof(table));

                const string query = @"
                    UPDATE Tables
                    SET DBTableName = @DBTableName, 
                        AdminTableName = @AdminTableName, 
                        AdminDescription = @AdminDescription
                    WHERE TableID = @TableID";

                return await _connection.ExecuteSafeAsync(query, table);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating table: {TableID}", table?.TableID);
                throw;
            }
        }

        // Delete a table
        public async Task<int> DeleteTableAsync(int tableId)
        {
            try
            {
                const string query = "DELETE FROM Tables WHERE TableID = @TableID";
                return await _connection.ExecuteSafeAsync(query, new { TableID = tableId });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error deleting table: {TableID}", tableId);
                throw;
            }
        }
    }
}