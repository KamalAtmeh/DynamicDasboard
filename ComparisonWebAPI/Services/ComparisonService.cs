using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;
using Dapper; // Add this import for Dapper extension methods
using DynamicDashboardCommon.Models;

namespace DynamicDasboardWebAPI.Services
{
    public class ComparisonService
    {
        private readonly string _connectionString;
        private readonly ILogger<ComparisonService> _logger;

        public ComparisonService(IConfiguration config, ILogger<ComparisonService> logger = null)
        {
            _connectionString = config.GetConnectionString("DefaultConnection")
                ?? throw new ArgumentNullException(nameof(config), "Connection string 'DefaultConnection' not found");
            _logger = logger;
        }

        /// <summary>
        /// Executes a SQL query asynchronously and returns the results as a list of dictionaries.
        /// </summary>
        /// <param name="query">The SQL query to execute.</param>
        /// <returns>A list of dictionaries representing the query results.</returns>
        public async Task<List<Dictionary<string, object>>> ExecuteQueryAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentException("Query cannot be empty", nameof(query));

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // Use Dapper to execute the query
                var result = await connection.QueryAsync(query);

                // Convert to list of dictionaries
                var dictResults = new List<Dictionary<string, object>>();
                foreach (var row in result)
                {
                    var dict = new Dictionary<string, object>();
                    foreach (var prop in (IDictionary<string, object>)row)
                    {
                        dict[prop.Key] = prop.Value;
                    }
                    dictResults.Add(dict);
                }

                return dictResults;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error executing query: {Query}", query);
                throw;
            }
        }

        /// <summary>
        /// Compares two datasets regardless of row or column order.
        /// </summary>
        /// <param name="dataset1">The first dataset to compare.</param>
        /// <param name="dataset2">The second dataset to compare.</param>
        /// <returns>True if the datasets are identical; otherwise, false.</returns>
        public bool CompareDatasets(List<Dictionary<string, object>> dataset1, List<Dictionary<string, object>> dataset2)
        {
            if (dataset1 == null) throw new ArgumentNullException(nameof(dataset1));
            if (dataset2 == null) throw new ArgumentNullException(nameof(dataset2));

            if (dataset1.Count != dataset2.Count)
                return false;

            try
            {
                // Convert each row to a string representation and compare the sets
                var set1 = dataset1.Select(NormalizeRow).ToHashSet();
                var set2 = dataset2.Select(NormalizeRow).ToHashSet();

                return set1.SetEquals(set2);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error comparing datasets");
                throw;
            }
        }

        /// <summary>
        /// Normalizes a row by creating a consistent string representation
        /// </summary>
        private string NormalizeRow(Dictionary<string, object> row)
        {
            return string.Join("|", row
                .OrderBy(kvp => kvp.Key)
                .Select(kvp => $"{kvp.Key}={FormatValue(kvp.Value)}"));
        }

        /// <summary>
        /// Formats a value to ensure consistent string representation
        /// </summary>
        private string FormatValue(object value)
        {
            if (value == null)
                return "NULL";

            // Handle dates consistently
            if (value is DateTime dateTime)
                return dateTime.ToString("yyyy-MM-dd HH:mm:ss.fff");

            // Handle decimals consistently
            if (value is decimal decimalValue)
                return decimalValue.ToString("0.################");

            return value.ToString();
        }
    }
}