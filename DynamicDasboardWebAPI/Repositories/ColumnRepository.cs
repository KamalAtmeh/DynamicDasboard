using Dapper;
using DynamicDashboardCommon.Models;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace DynamicDasboardWebAPI.Repositories
{
    /// <summary>
    /// Repository class for managing columns in the database.
    /// Provides methods to fetch, add, update, and delete columns.
    /// </summary>
    public class ColumnRepository
    {
        private readonly IDbConnection _connection;

        /// <summary>
        /// Initializes a new instance of the <see cref="ColumnRepository"/> class.
        /// </summary>
        /// <param name="connection">The database connection to be used by the repository.</param>
        public ColumnRepository(IDbConnection connection)
        {
            _connection = connection;
        }

        /// <summary>
        /// Fetches columns for a specific table.
        /// </summary>
        /// <param name="tableId">The ID of the table whose columns are to be fetched.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a collection of columns.</returns>
        public async Task<IEnumerable<Column>> GetColumnsByTableIdAsync(int tableId)
        {
            string query = "SELECT * FROM Columns WHERE TableID = @TableID";
            return await _connection.QueryAsync<Column>(query, new { TableID = tableId });
        }

        /// <summary>
        /// Adds a new column to the database.
        /// </summary>
        /// <param name="column">The column to be added.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the number of rows affected.</returns>
        public async Task<int> AddColumnAsync(Column column)
        {
            string query = @"
                    INSERT INTO Columns (TableID, DBColumnName, AdminColumnName, DataType, IsNullable, AdminDescription)
                    VALUES (@TableID, @DBColumnName, @AdminColumnName, @DataType, @IsNullable, @AdminDescription)";
            return await _connection.ExecuteAsync(query, column);
        }

        /// <summary>
        /// Updates an existing column in the database.
        /// </summary>
        /// <param name="column">The column to be updated.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the number of rows affected.</returns>
        public async Task<int> UpdateColumnAsync(Column column)
        {
            string query = @"
                    UPDATE Columns
                    SET DBColumnName = @DBColumnName, AdminColumnName = @AdminColumnName, 
                        DataType = @DataType, IsNullable = @IsNullable, AdminDescription = @AdminDescription
                    WHERE ColumnID = @ColumnID";
            return await _connection.ExecuteAsync(query, column);
        }

        /// <summary>
        /// Deletes a column from the database.
        /// </summary>
        /// <param name="columnId">The ID of the column to be deleted.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the number of rows affected.</returns>
        public async Task<int> DeleteColumnAsync(int columnId)
        {
            string query = "DELETE FROM Columns WHERE ColumnID = @ColumnID";
            return await _connection.ExecuteAsync(query, new { ColumnID = columnId });
        }
    }
}