//using System;
//using System.Collections.Generic;
//using System.Threading.Tasks;
//using Dapper;
//using DynamicDasboardWebAPI.Utilities;
//using Microsoft.Extensions.Logging;

//namespace DynamicDasboardWebAPI.Services
//{
//    /// <summary>
//    /// Implementation of IDataAccessService for generic database operations
//    /// </summary>
//    public class DataAccessService : IDataAccessService
//    {
//        private readonly DbConnectionFactory _connectionFactory;
//        private readonly ILogger<DataAccessService> _logger;

//        public DataAccessService(DbConnectionFactory connectionFactory, ILogger<DataAccessService> logger)
//        {
//            _connectionFactory = connectionFactory;
//            _logger = logger;
//        }

//        /// <inheritdoc/>
//        public async Task<IEnumerable<T>> QueryAsync<T>(string dbType, string query, object parameters = null)
//        {
//            try
//            {
//                using var connection = await _connectionFactory.CreateOpenConnectionAsync(dbType);
//                return await connection.QueryAsync<T>(query, parameters);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error executing query: {Query}", query);
//                throw new DatabaseException("Error executing query", ex);
//            }
//        }

//        /// <inheritdoc/>
//        public async Task<T> QueryFirstOrDefaultAsync<T>(string dbType, string query, object parameters = null)
//        {
//            try
//            {
//                using var connection = await _connectionFactory.CreateOpenConnectionAsync(dbType);
//                return await connection.QueryFirstOrDefaultAsync<T>(query, parameters);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error executing query: {Query}", query);
//                throw new DatabaseException("Error executing query", ex);
//            }
//        }

//        /// <inheritdoc/>
//        public async Task<int> ExecuteAsync(string dbType, string command, object parameters = null)
//        {
//            try
//            {
//                using var connection = await _connectionFactory.CreateOpenConnectionAsync(dbType);
//                return await connection.ExecuteAsync(command, parameters);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error executing command: {Command}", command);
//                throw new DatabaseException("Error executing command", ex);
//            }
//        }

//        /// <inheritdoc/>
//        public async Task<T> ExecuteScalarAsync<T>(string dbType, string query, object parameters = null)
//        {
//            try
//            {
//                using var connection = await _connectionFactory.CreateOpenConnectionAsync(dbType);
//                return await connection.ExecuteScalarAsync<T>(query, parameters);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error executing scalar query: {Query}", query);
//                throw new DatabaseException("Error executing scalar query", ex);
//            }
//        }
//    }


//}