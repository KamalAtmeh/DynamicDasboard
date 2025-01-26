using Dapper;
using DynamicDashboardCommon.Models;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace DynamicDasboardWebAPI.Repositories
{
    public class ColumnRepository
    {
        private readonly IDbConnection _connection;

        public ColumnRepository(IDbConnection connection)
        {
            _connection = connection;
        }

        // Fetch columns for a specific table
        public async Task<IEnumerable<Column>> GetColumnsByTableIdAsync(int tableId)
        {
            string query = "SELECT * FROM Columns WHERE TableID = @TableID";
            return await _connection.QueryAsync<Column>(query, new { TableID = tableId });
        }

        // Add a new column
        public async Task<int> AddColumnAsync(Column column)
        {
            string query = @"
                INSERT INTO Columns (TableID, DBColumnName, AdminColumnName, DataType, IsNullable, AdminDescription)
                VALUES (@TableID, @DBColumnName, @AdminColumnName, @DataType, @IsNullable, @AdminDescription)";
            return await _connection.ExecuteAsync(query, column);
        }

        // Update an existing column
        public async Task<int> UpdateColumnAsync(Column column)
        {
            string query = @"
                UPDATE Columns
                SET DBColumnName = @DBColumnName, AdminColumnName = @AdminColumnName, 
                    DataType = @DataType, IsNullable = @IsNullable, AdminDescription = @AdminDescription
                WHERE ColumnID = @ColumnID";
            return await _connection.ExecuteAsync(query, column);
        }

        // Delete a column
        public async Task<int> DeleteColumnAsync(int columnId)
        {
            string query = "DELETE FROM Columns WHERE ColumnID = @ColumnID";
            return await _connection.ExecuteAsync(query, new { ColumnID = columnId });
        }
    }
}