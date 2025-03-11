using Dapper;
using DynamicDashboardCommon.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using DynamicDasboardWebAPI.Utilities;

namespace DynamicDasboardWebAPI.Repositories
{
    public class ColumnRepository : BaseRepository
    {
        public ColumnRepository(IDbConnection appDbConnection, DbConnectionFactory connectionFactory)
            : base(appDbConnection, connectionFactory)
        {
        }

        /// <summary>
        /// Fetch columns for a specific table.
        /// </summary>
        /// <param name="tableId">The table ID.</param>
        /// <param name="databaseId">Optional: ID of the DB to connect to (0=default).</param>
        public async Task<IEnumerable<Column>> GetColumnsByTableIdAsync(int tableId, int databaseId = 0)
        {
            try
            {
                const string query = "SELECT * FROM Columns WHERE TableID = @TableID";

                return await WithConnectionAsync(async conn =>
                {
                    return await conn.QuerySafeAsync<Column>(query, new { TableID = tableId });
                }, databaseId);
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Get a specific column by ID.
        /// </summary>
        /// <param name="columnId">The column ID.</param>
        /// <param name="databaseId">Optional: ID of the DB to connect to (0=default).</param>
        public async Task<Column> GetColumnByIdAsync(int columnId, int databaseId = 0)
        {
            try
            {
                const string query = "SELECT * FROM Columns WHERE ColumnID = @ColumnID";

                return await WithConnectionAsync(async conn =>
                {
                    return await conn.QueryFirstOrDefaultSafeAsync<Column>(query, new { ColumnID = columnId });
                }, databaseId);
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Add a new column.
        /// </summary>
        /// <param name="column">The column object to add.</param>
        /// <param name="databaseId">Optional: ID of the DB to connect to (0=default).</param>
        public async Task<int> AddColumnAsync(Column column, int databaseId = 0)
        {
            try
            {
                if (column == null) throw new ArgumentNullException(nameof(column));

                const string query = @"
                    INSERT INTO Columns (
                        TableID, 
                        DBColumnName, 
                        AdminColumnName, 
                        DataType, 
                        IsNullable, 
                        AdminDescription, 
                        IsLookupColumn
                    )
                    VALUES (
                        @TableID, 
                        @DBColumnName, 
                        @AdminColumnName, 
                        @DataType, 
                        @IsNullable, 
                        @AdminDescription, 
                        @IsLookupColumn
                    );
                    SELECT CAST(SCOPE_IDENTITY() as int);";

                return await WithConnectionAsync(async conn =>
                {
                    return await conn.ExecuteScalarSafeAsync<int>(query, column);
                }, databaseId);
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Update an existing column.
        /// </summary>
        /// <param name="column">The column object with updated data.</param>
        /// <param name="databaseId">Optional: ID of the DB to connect to (0=default).</param>
        public async Task<int> UpdateColumnAsync(Column column, int databaseId = 0)
        {
            try
            {
                if (column == null) throw new ArgumentNullException(nameof(column));

                const string query = @"
                    UPDATE Columns
                    SET
                        DBColumnName = @DBColumnName,
                        AdminColumnName = @AdminColumnName,
                        DataType = @DataType,
                        IsNullable = @IsNullable,
                        AdminDescription = @AdminDescription,
                        IsLookupColumn = @IsLookupColumn
                    WHERE ColumnID = @ColumnID";

                return await WithConnectionAsync(async conn =>
                {
                    return await conn.ExecuteSafeAsync(query, column);
                }, databaseId);
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Delete a column.
        /// </summary>
        /// <param name="columnId">The column ID to delete.</param>
        /// <param name="databaseId">Optional: ID of the DB to connect to (0=default).</param>
        public async Task<int> DeleteColumnAsync(int columnId, int databaseId = 0)
        {
            try
            {
                const string query = "DELETE FROM Columns WHERE ColumnID = @ColumnID";

                return await WithConnectionAsync(async conn =>
                {
                    return await conn.ExecuteSafeAsync(query, new { ColumnID = columnId });
                }, databaseId);
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}