using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using DynamicDashboardCommon.Models;
using Microsoft.Extensions.Configuration;

namespace DynamicDasboardWebAPI.Services
{
    /// <summary>
    /// Service for executing SQL queries and comparing datasets.
    /// </summary>
    public class ComparisonService
    {
        private readonly string _connectionString;

        /// <summary>
        /// Initializes a new instance of the <see cref="ComparisonService"/> class.
        /// </summary>
        /// <param name="config">The configuration instance used to retrieve the connection string.</param>
        public ComparisonService(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection");
        }

        /// <summary>
        /// Executes a SQL query asynchronously and returns the results as a list of dictionaries.
        /// </summary>
        /// <param name="query">The SQL query to execute.</param>
        /// <returns>A list of dictionaries representing the query results.</returns>
        public async Task<List<Dictionary<string, object>>> ExecuteQueryAsync(string query)
        {
            var results = new List<Dictionary<string, object>>();

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
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                row[reader.GetName(i)] = reader.GetValue(i);
                            }
                            results.Add(row);
                        }
                    }
                }
            }

            return results;
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
            {
                return false;
            }

            foreach (var row1 in dataset1)
            {
                bool matchFound = false;
                foreach (var row2 in dataset2)
                {
                    if (DictionariesEqual(row1, row2))
                    {
                        matchFound = true;
                        break;
                    }
                }
                if (!matchFound)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Compares two dictionaries for equality.
        /// </summary>
        /// <param name="dict1">The first dictionary to compare.</param>
        /// <param name="dict2">The second dictionary to compare.</param>
        /// <returns>True if the dictionaries are equal; otherwise, false.</returns>
        private bool DictionariesEqual(Dictionary<string, object> dict1, Dictionary<string, object> dict2)
        {
            if (dict1.Count != dict2.Count)
            {
                return false;
            }

            foreach (var kvp in dict1)
            {
                if (!dict2.TryGetValue(kvp.Key, out var value) || !Equals(kvp.Value, value))
                {
                    return false;
                }
            }

            return true;
        }
    }
}