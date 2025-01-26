using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using DynamicDashboardCommon.Models;
using Microsoft.Extensions.Configuration;

namespace DynamicDasboardWebAPI.Services
{
    public class ComparisonService
    {
        private readonly string _connectionString;

        public ComparisonService(IConfiguration config)
        {
            // Get the database connection string from appsettings.json
            _connectionString = config.GetConnectionString("DefaultConnection");
        }

        /// <summary>
        /// Executes a SQL query asynchronously and returns the results as a list of dictionaries.
        /// </summary>
        /// <param name="query">The SQL query to execute.</param>
        /// <returns>A list of dictionaries representing the query results.</returns>
        public async Task<List<Dictionary<string, object>>> ExecuteQueryAsync(string query)
        {
            var result = new List<Dictionary<string, object>>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand(query, connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
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
            }

            return result;
        }

        /// <summary>
        /// Compares two datasets regardless of row or column order.
        /// </summary>
        /// <param name="dataset1">The first dataset to compare.</param>
        /// <param name="dataset2">The second dataset to compare.</param>
        /// <returns>True if the datasets are identical; otherwise, false.</returns>
        public bool CompareDatasets(List<Dictionary<string, object>> dataset1, List<Dictionary<string, object>> dataset2)
        {
            if (dataset1.Count != dataset2.Count)
                return false;

            // Convert each row to a string representation and compare the sets
            var set1 = dataset1.Select(row => string.Join("|", row.OrderBy(kvp => kvp.Key).Select(kvp => $"{kvp.Key}={kvp.Value}"))).ToHashSet();
            var set2 = dataset2.Select(row => string.Join("|", row.OrderBy(kvp => kvp.Key).Select(kvp => $"{kvp.Key}={kvp.Value}"))).ToHashSet();

            return set1.SetEquals(set2);
        }
    }
}