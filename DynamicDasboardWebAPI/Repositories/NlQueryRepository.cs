using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using DynamicDashboardCommon.Models;

namespace DynamicDasboardWebAPI.Repositories
{
    public class NlQueryRepository
    {
        private readonly IDbConnection _connection;

        public NlQueryRepository(IDbConnection connection)
        {
            _connection = connection;
        }

        /// <summary>
        /// Executes a SQL query and returns the results as a list of dictionaries.
        /// </summary>
        /// <param name="query">The SQL query to execute.</param>
        /// <returns>A list of dictionaries, where each dictionary represents a row of data.</returns>
        public async Task<List<Dictionary<string, object>>> ExecuteQueryAsync(string query)
        {
            var result = new List<Dictionary<string, object>>();

            using (var command = _connection.CreateCommand())
            {
                command.CommandText = query;

                // Use synchronous version instead of async
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var row = new Dictionary<string, object>();
                        for (var i = 0; i < reader.FieldCount; i++)
                        {
                            row[reader.GetName(i)] = reader.GetValue(i);
                        }
                        result.Add(row);
                    }
                }
            }

            return result;
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