using Dapper;
using DynamicDashboardCommon.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using DynamicDasboardWebAPI.Utilities;

namespace DynamicDasboardWebAPI.Repositories
{
    public class TableRepository : BaseRepository
    {
        public TableRepository(
            IDbConnection connection,
            DbConnectionFactory connectionFactory)
            : base(connection, connectionFactory)
        {
        }

        /// <summary>
        /// Fetch tables for a specific database (default DB).
        /// Note: The 'databaseId' parameter here is part of the entity logic (i.e., which DB row?), 
        /// not the dynamic DB connection ID.
        /// </summary>
        public async Task<IEnumerable<Table>> GetTablesByDatabaseIdAsync(int databaseId)
        {
            try
            {
                const string query = "SELECT * FROM Tables WHERE DatabaseID = @DatabaseID";

                return await WithConnectionAsync(async conn =>
                {
                    // The 'databaseId' here is just used in the WHERE clause, not for a dynamic connection
                    return await conn.QuerySafeAsync<Table>(query, new { DatabaseID = databaseId });
                });
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Get a specific table by ID (default DB).
        /// </summary>
        public async Task<Table> GetTableByIdAsync(int tableId)
        {
            try
            {
                const string query = "SELECT * FROM Tables WHERE TableID = @TableID";

                return await WithConnectionAsync(async conn =>
                {
                    return await conn.QueryFirstOrDefaultSafeAsync<Table>(query, new { TableID = tableId });
                });
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Add a new table (default DB).
        /// </summary>
        public async Task<int> AddTableAsync(Table table)
        {
            try
            {
                const string query = @"
                    INSERT INTO Tables (DatabaseID, DBTableName, AdminTableName, AdminDescription)
                    VALUES (@DatabaseID, @DBTableName, @AdminTableName, @AdminDescription);
                    SELECT CAST(SCOPE_IDENTITY() as int)";

                return await WithConnectionAsync(async conn =>
                {
                    return await conn.ExecuteScalarSafeAsync<int>(query, table);
                });
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Update an existing table (default DB).
        /// </summary>
        public async Task<int> UpdateTableAsync(Table table)
        {
            try
            {
                const string query = @"
                    UPDATE Tables
                    SET 
                        DBTableName = @DBTableName, 
                        AdminTableName = @AdminTableName, 
                        AdminDescription = @AdminDescription
                    WHERE TableID = @TableID";

                return await WithConnectionAsync(async conn =>
                {
                    return await conn.ExecuteSafeAsync(query, table);
                });
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Delete a table (default DB).
        /// </summary>
        public async Task<int> DeleteTableAsync(int tableId)
        {
            try
            {
                const string query = @"
                    UPDATE Tables
                    SET 
                        IsActive = 0
                    WHERE TableID = @TableID";

                return await WithConnectionAsync(async conn =>
                {
                    return await conn.ExecuteSafeAsync(query, new { TableID = tableId });
                });
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
