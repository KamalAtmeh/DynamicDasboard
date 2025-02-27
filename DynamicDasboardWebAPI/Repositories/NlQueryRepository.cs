using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using DynamicDasboardWebAPI.Utilities;
using DynamicDashboardCommon.Models;
using Microsoft.AspNetCore.Connections;
using Microsoft.Data.SqlClient;

namespace DynamicDasboardWebAPI.Repositories
{
    public class NlQueryRepository
    {
        private readonly IDbConnection _connection;
        private readonly DbConnectionFactory _connectionFactory;
        private readonly DatabaseRepository _databaseRepository;

        public NlQueryRepository(
            IDbConnection connection,
            DbConnectionFactory connectionFactory,
            DatabaseRepository databaseRepository)
        {
            _connection = connection;
            _connectionFactory = connectionFactory;
            _databaseRepository = databaseRepository;
        }

        /// <summary>
        /// Executes a SQL query and returns the results as a list of dictionaries.
        /// </summary>
        /// <param name="query">The SQL query to execute.</param>
        /// <returns>A list of dictionaries, where each dictionary represents a row of data.</returns>
        public async Task<List<Dictionary<string, object>>> ExecuteQueryAsync(string query)
        {
            var result = new List<Dictionary<string, object>>();

            try
            {
                // Option 1: Use Dapper with the project's extension methods
                var data = await _connection.QuerySafeAsync<dynamic>(query);
                foreach (var item in data)
                {
                    var row = new Dictionary<string, object>();
                    foreach (var prop in ((IDictionary<string, object>)item))
                    {
                        row[prop.Key] = prop.Value;
                    }
                    result.Add(row);
                }
            }
            catch (Exception ex)
            {
                // Log the exception - use the logging pattern found elsewhere in the project
                throw new DatabaseException($"Error executing query: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Executes a query on a specific database using its ID
        /// </summary>
        /// <param name="query">SQL query to execute</param>
        /// <param name="databaseId">ID of the database in the system</param>
        /// <returns>Query results as a list of dictionaries</returns>
        public async Task<List<Dictionary<string, object>>> ExecuteQueryOnDatabaseAsync(string query, int databaseId)
        {
            // Get the database information
            var database = await _databaseRepository.GetDatabaseByIdAsync(databaseId);
            if (database == null)
            {
                throw new ArgumentException($"Database with ID {databaseId} not found");
            }

            // Get database type name
            string dbTypeName = GetDatabaseTypeName(database.DatabaseID);

            // Use the connection factory to create a connection for this specific database
            using var connection = _connectionFactory.CreateConnection(dbTypeName);
            connection.ConnectionString = database.ConnectionString;

            try
            {
                connection.Open();

                var result = new List<Dictionary<string, object>>();
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

                return result;
            }
            catch (Exception ex)
            {
                throw new DatabaseException($"Error executing query on {database.Name}: {ex.Message}", ex);
            }
        }

        private string GetDatabaseTypeName(int typeId)
        {
            return typeId switch
            {
                1 => "SQLServer",
                2 => "MySQL",
                3 => "Oracle",
                4 => "SQLServer2",
                _ => throw new ArgumentException($"Unsupported database type ID: {typeId}")
            };
        }

        /// <summary>
        /// Retrieves metadata for a database including tables and columns.
        /// </summary>
        /// <returns>A dictionary containing database metadata.</returns>
        public async Task<Dictionary<string, object>> GetDatabaseMetadataAsync(int databaseId)
        {
            // This would be implemented to retrieve database metadata from your application's database
            // For now, we'll return an empty dictionary
            return new Dictionary<string, object>();
        }
    }
}