using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using DynamicDasboardWebAPI.Utilities;
using DynamicDashboardCommon.Models;
using Microsoft.Extensions.Logging;

namespace DynamicDasboardWebAPI.Repositories
{
    /// <summary>
    /// Repository for executing dynamic SQL queries.
    /// </summary>
    public class QueryRepository
    {
        private readonly IDbConnection _connection;
        private readonly DbConnectionFactory _connectionFactory;
        private readonly ILogger<QueryRepository> _logger;

        public QueryRepository(
            IDbConnection connection,
            DbConnectionFactory connectionFactory,
            ILogger<QueryRepository> logger = null)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _logger = logger;
        }

        /// <summary>
        /// Executes a dynamic SQL query using the default connection and returns the result.
        /// </summary>
        public async Task<dynamic> ExecuteQueryAsync(string query)
        {
            try
            {
                return await _connection.QuerySafeAsync<dynamic>(query);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error executing query: {Query}", query);
                throw;
            }
        }

        /// <summary>
        /// Executes a dynamic SQL query on the specified database and returns the result.
        /// </summary>
        public async Task<dynamic> ExecuteQueryAsync(string query, int databaseId)
        {


            try
            {
                using var connection = await _connectionFactory.CreateOpenConnectionAsync(databaseId);
                return await connection.QuerySafeAsync<dynamic>(query);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error executing query on database {DatabaseId}: {Query}", databaseId, query);
                throw;
            }
        }

        /// <summary>
        /// Executes a dynamic SQL query with parameters and returns the result.
        /// </summary>
        public async Task<dynamic> ExecuteQueryWithParamsAsync(string query, object parameters)
        {


            try
            {
                return await _connection.QuerySafeAsync<dynamic>(query, parameters);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error executing parameterized query: {Query}", query);
                throw;
            }
        }

        /// <summary>
        /// Executes a dynamic SQL query with parameters on the specified database.
        /// </summary>
        public async Task<dynamic> ExecuteQueryWithParamsAsync(string query, int databaseId, object parameters)
        {
            try
            {
                using var connection = await _connectionFactory.CreateOpenConnectionAsync(databaseId);
                return await connection.QuerySafeAsync<dynamic>(query, parameters);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error executing parameterized query on database {DatabaseId}: {Query}", databaseId, query);
                throw;
            }
        }
    }
}