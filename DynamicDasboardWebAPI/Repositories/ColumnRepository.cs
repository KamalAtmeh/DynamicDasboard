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
    public class ColumnRepository
    {
        private readonly IDbConnection _connection;
        private readonly ILogger<ColumnRepository> _logger;

        public ColumnRepository(IDbConnection connection, ILogger<ColumnRepository> logger = null)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _logger = logger;
        }

        // Fetch columns for a specific table
        public async Task<IEnumerable<Column>> GetColumnsByTableIdAsync(int tableId)
        {
            try
            {
                const string query = "SELECT * FROM Columns WHERE TableID = @TableID";
                return await _connection.QuerySafeAsync<Column>(query, new { TableID = tableId });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving columns for table: {TableID}", tableId);
                throw;
            }
        }

        // Get a specific column by ID
        public async Task<Column> GetColumnByIdAsync(int columnId)
        {
            try
            {
                const string query = "SELECT * FROM Columns WHERE ColumnID = @ColumnID";
                return await _connection.QueryFirstOrDefaultSafeAsync<Column>(query, new { ColumnID = columnId });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving column by ID: {ColumnID}", columnId);
                throw;
            }
        }

        // Add a new column
        public async Task<int> AddColumnAsync(Column column)
        {
            try
            {
                if (column == null) throw new ArgumentNullException(nameof(column));

                const string query = @"
                    INSERT INTO Columns (TableID, DBColumnName, AdminColumnName, DataType, IsNullable, AdminDescription)
                    VALUES (@TableID, @DBColumnName, @AdminColumnName, @DataType, @IsNullable, @AdminDescription);
                    SELECT CAST(SCOPE_IDENTITY() as int)";

                return await _connection.ExecuteScalarSafeAsync<int>(query, column);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error adding column: {DBColumnName}", column?.DBColumnName);
                throw;
            }
        }

        // Update an existing column
        public async Task<int> UpdateColumnAsync(Column column)
        {
            try
            {
                if (column == null) throw new ArgumentNullException(nameof(column));

                const string query = @"
                    UPDATE Columns
                    SET DBColumnName = @DBColumnName, 
                        AdminColumnName = @AdminColumnName, 
                        DataType = @DataType, 
                        IsNullable = @IsNullable, 
                        AdminDescription = @AdminDescription
                    WHERE ColumnID = @ColumnID";

                return await _connection.ExecuteSafeAsync(query, column);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating column: {ColumnID}", column?.ColumnID);
                throw;
            }
        }

        // Delete a column
        public async Task<int> DeleteColumnAsync(int columnId)
        {
            try
            {
                const string query = "DELETE FROM Columns WHERE ColumnID = @ColumnID";
                return await _connection.ExecuteSafeAsync(query, new { ColumnID = columnId });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error deleting column: {ColumnID}", columnId);
                throw;
            }
        }
    }
}