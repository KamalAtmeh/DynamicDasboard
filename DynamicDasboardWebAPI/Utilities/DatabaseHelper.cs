using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;

namespace DynamicDasboardWebAPI.Utilities
{
    /// <summary>
    /// Helper class for standardized database operations using Dapper
    /// </summary>
    public static class DatabaseHelper
    {
        /// <summary>
        /// Executes a query safely and maps the result to a list of entities
        /// </summary>
        public static async Task<IEnumerable<T>> QuerySafeAsync<T>(this IDbConnection connection, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            try
            {
                return await connection.QueryAsync<T>(sql, param, transaction, commandTimeout, commandType);
            }
            catch (Exception ex)
            {
                throw new DatabaseException("Error executing query", ex);
            }
        }

        /// <summary>
        /// Executes a command safely and returns the number of affected rows
        /// </summary>
        public static async Task<int> ExecuteSafeAsync(this IDbConnection connection, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            try
            {
                return await connection.ExecuteAsync(sql, param, transaction, commandTimeout, commandType);
            }
            catch (Exception ex)
            {
                throw new DatabaseException("Error executing command", ex);
            }
        }

        /// <summary>
        /// Executes a query on a specific database and maps the results to a dictionary
        /// </summary>
        public static async Task<List<Dictionary<string, object>>> ExecuteQueryOnDatabaseAsync(
            this DbConnectionFactory connectionFactory,
            string dbType,
            string connectionString,
            string query)
        {
            var result = new List<Dictionary<string, object>>();

            try
            {
                // Create a new connection for the specified database
                using (var connection = connectionFactory.CreateConnection(dbType))
                {
                    // Set the connection string explicitly
                    connection.ConnectionString = connectionString;

                    // Open the connection
                    connection.Open();

                    // Execute the query
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = query;
                        command.CommandTimeout = 30;

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var row = new Dictionary<string, object>();
                                for (var i = 0; i < reader.FieldCount; i++)
                                {
                                    var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                                    row[reader.GetName(i)] = value;
                                }
                                result.Add(row);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new DatabaseException($"Error executing query on {dbType} database: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Executes a query safely and returns the first result or default value
        /// </summary>
        public static async Task<T> QueryFirstOrDefaultSafeAsync<T>(this IDbConnection connection, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            try
            {
                return await connection.QueryFirstOrDefaultAsync<T>(sql, param, transaction, commandTimeout, commandType);
            }
            catch (Exception ex)
            {
                throw new DatabaseException("Error executing query", ex);
            }
        }

        /// <summary>
        /// Executes a scalar query safely and returns the result
        /// </summary>
        public static async Task<T> ExecuteScalarSafeAsync<T>(this IDbConnection connection, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            try
            {
                return await connection.ExecuteScalarAsync<T>(sql, param, transaction, commandTimeout, commandType);
            }
            catch (Exception ex)
            {
                throw new DatabaseException("Error executing scalar query", ex);
            }
        }
    }

    /// <summary>
    /// Custom exception for database operations
    /// </summary>
    public class DatabaseException : Exception
    {
        public DatabaseException(string message, Exception innerException = null)
            : base(message, innerException)
        {
        }
    }
}