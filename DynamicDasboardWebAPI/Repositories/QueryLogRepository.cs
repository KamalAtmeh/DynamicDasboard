using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using DynamicDashboardCommon.Models;
using Microsoft.Extensions.Logging;
using DynamicDasboardWebAPI.Utilities;

namespace DynamicDasboardWebAPI.Repositories
{
    /// <summary>
    /// Repository for logging executed queries into the QueryLogs table.
    /// </summary>
    public class QueryLogsRepository
    {
        private readonly IDbConnection _connection;
        private readonly ILogger<QueryLogsRepository> _logger;

        public QueryLogsRepository(IDbConnection connection, ILogger<QueryLogsRepository> logger = null)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _logger = logger;
        }

        /// <summary>
        /// Logs an executed query into the QueryLogs table.
        /// </summary>
        /// <param name="queryText">The SQL query executed.</param>
        /// <param name="executedBy">User ID of the executor (nullable).</param>
        /// <param name="databaseType">The type of database used.</param>
        /// <param name="result">Serialized result of the query (optional).</param>
        /// <returns>The number of rows affected.</returns>
        public async Task<int> LogQueryAsync(string queryText, int? executedBy, string databaseType, string result)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(queryText))
                    throw new ArgumentException("Query text cannot be empty", nameof(queryText));

                if (string.IsNullOrWhiteSpace(databaseType))
                    throw new ArgumentException("Database type cannot be empty", nameof(databaseType));

                const string sql = @"
                    INSERT INTO QueryLogs (QueryText, ExecutedAt, ExecutedBy, DatabaseType, Result)
                    VALUES (@QueryText, GETDATE(), @ExecutedBy, @DatabaseType, @Result)";

                return await _connection.ExecuteSafeAsync(sql, new
                {
                    QueryText = queryText,
                    ExecutedBy = executedBy,
                    DatabaseType = databaseType,
                    Result = result
                });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error logging query execution: {QueryText}", queryText);
                throw;
            }
        }

        /// <summary>
        /// Gets the history of executed queries.
        /// </summary>
        /// <param name="userId">Optional user ID to filter by.</param>
        /// <param name="limit">Number of records to retrieve.</param>
        /// <returns>List of query log entries.</returns>
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

                return await _connection.QuerySafeAsync<Query>(sql, parameters);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving query history");
                throw;
            }
        }

        /// <summary>
        /// Gets details of a specific query by ID.
        /// </summary>
        /// <param name="queryId">The ID of the query to retrieve.</param>
        /// <returns>The query details.</returns>
        public async Task<Query> GetQueryByIdAsync(int queryId)
        {
            try
            {
                const string sql = "SELECT * FROM QueryLogs WHERE QueryID = @QueryID";
                return await _connection.QueryFirstOrDefaultSafeAsync<Query>(sql, new { QueryID = queryId });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving query by ID: {QueryID}", queryId);
                throw;
            }
        }
    }
}