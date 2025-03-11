using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using DynamicDasboardWebAPI.Utilities;
using DynamicDashboardCommon.Models;

namespace DynamicDasboardWebAPI.Repositories
{
    /// <summary>
    /// Repository for logging executed queries into the QueryLogs table.
    /// </summary>
    public class QueryLogsRepository : BaseRepository
    {

        public QueryLogsRepository(
            IDbConnection appDbConnection,
            DbConnectionFactory connectionFactory)
            : base(appDbConnection, connectionFactory)
        {

        }

        /// <summary>
        /// Logs an executed query into the QueryLogs table (default DB).
        /// The parameter 'databaseID' here is stored in 'DatabaseType' column, 
        /// not used for dynamic connections.
        /// </summary>
        public async Task<int> LogQueryAsync(string queryText, int? executedBy, int databaseID, string result)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(queryText))
                    throw new ArgumentException("Query text cannot be empty", nameof(queryText));

                const string sql = @"
                    INSERT INTO QueryLogs (QueryText, ExecutedAt, ExecutedBy, DatabaseType, Result)
                    VALUES (@QueryText, GETDATE(), @ExecutedBy, @DatabaseType, @Result)";

                return await WithConnectionAsync(async conn =>
                {
                    return await conn.ExecuteSafeAsync(sql, new
                    {
                        QueryText = queryText,
                        ExecutedBy = executedBy,
                        DatabaseType = databaseID,
                        Result = result
                    });
                });
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Gets the history of executed queries (default DB).
        /// </summary>
        public async Task<IEnumerable<Query>> GetQueryHistoryAsync(int? userId = null, int limit = 100)
        {
            try
            {
                string sql;
                object parameters;

                if (userId.HasValue)
                {
                    sql = "SELECT TOP (@Limit) * FROM QueryLogs WHERE ExecutedBy = @UserId ORDER BY ExecutedAt DESC";
                    parameters = new { UserId = userId.Value, Limit = limit };
                }
                else
                {
                    sql = "SELECT TOP (@Limit) * FROM QueryLogs ORDER BY ExecutedAt DESC";
                    parameters = new { Limit = limit };
                }

                return await WithConnectionAsync(async conn =>
                {
                    return await conn.QuerySafeAsync<Query>(sql, parameters);
                });
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Gets details of a specific query by ID (default DB).
        /// </summary>
        public async Task<Query> GetQueryByIdAsync(int queryId)
        {
            try
            {
                const string sql = "SELECT * FROM QueryLogs WHERE QueryID = @QueryID";

                return await WithConnectionAsync(async conn =>
                {
                    return await conn.QueryFirstOrDefaultSafeAsync<Query>(sql, new { QueryID = queryId });
                });
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}