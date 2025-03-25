using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DynamicDashboardCommon.Models.TestAutomation;
using Microsoft.Extensions.Configuration;

namespace DynamicDasboardWebAPI.Services.TestAutomation
{
    /// <summary>
    /// Service for comparing datasets with enhanced column matching and strict value comparison.
    /// Implements the binary comparison logic where datasets are either 100% identical or 0% identical
    /// based on common columns and values.
    /// </summary>
    public class DatasetComparisonService
    {
        private readonly IConfiguration _configuration;
        private readonly int _maxRecordsToCompare;

        /// <summary>
        /// Initializes a new instance of the DatasetComparisonService class.
        /// </summary>
        /// <param name="configuration">Application configuration.</param>
        public DatasetComparisonService(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _maxRecordsToCompare = _configuration.GetValue<int>("TestAutomation:MaxRecordsToCompare", 100);
        }

        /// <summary>
        /// Compares two datasets and determines if they are identical based on common columns.
        /// If any row has a different value for common columns, the datasets are considered 0% identical.
        /// </summary>
        /// <param name="expected">The expected dataset.</param>
        /// <param name="actual">The actual dataset.</param>
        /// <param name="maxRecords">Optional maximum records to compare.</param>
        /// <returns>A detailed comparison result.</returns>
        public DatasetComparisonResult CompareDatasets(
            List<Dictionary<string, object>> expected,
            List<Dictionary<string, object>> actual,
            int? maxRecords = null, bool orderIndependent = true)
        {
            var result = new DatasetComparisonResult();

            // Handle null or empty datasets
            if (expected == null) expected = new List<Dictionary<string, object>>();
            if (actual == null) actual = new List<Dictionary<string, object>>();

            // Set result datasets
            result.Expected = expected;
            result.Actual = actual;

            // Early exit for empty datasets
            if (!expected.Any() || !actual.Any())
            {
                bool bothEmpty = !expected.Any() && !actual.Any();
                result.IsEquivalent = bothEmpty; // Both empty is considered matching

                // Update the comparison summary for empty datasets
                if (bothEmpty)
                {
                    result.ComparisonSummary = "Both datasets are empty";
                }
                else if (!expected.Any())
                {
                    result.ComparisonSummary = "Expected dataset is empty";
                }
                else
                {
                    result.ComparisonSummary = "Actual dataset is empty";
                }

                return result;
            }

            // Get normalized column names from both datasets
            var expectedColumns = expected[0].Keys
                .Select(key => NormalizeColumnName(key))
                .ToList();

            var actualColumns = actual[0].Keys
                .Select(key => NormalizeColumnName(key))
                .ToList();

            // Create a mapping between normalized column names and original names
            // Using StringComparer.OrdinalIgnoreCase for case-insensitive column matching
            var expectedColumnMapping = expected[0].Keys
                .ToDictionary(key => NormalizeColumnName(key), key => key, StringComparer.OrdinalIgnoreCase);

            var actualColumnMapping = actual[0].Keys
                .ToDictionary(key => NormalizeColumnName(key), key => key, StringComparer.OrdinalIgnoreCase);

            // Find common columns based on normalized names - using case-insensitive comparison
            var commonNormalizedColumns = expectedColumns
                .Intersect(actualColumns, StringComparer.OrdinalIgnoreCase)
                .ToList();

            // Store original column names for display
            result.CommonColumns = commonNormalizedColumns
                .Select(normalizedName => expectedColumnMapping.TryGetValue(normalizedName, out var originalName) ? originalName : normalizedName)
                .ToList();

            result.UniqueExpectedColumns = expectedColumns
                .Except(commonNormalizedColumns, StringComparer.OrdinalIgnoreCase)
                .Select(normalizedName => expectedColumnMapping.TryGetValue(normalizedName, out var originalName) ? originalName : normalizedName)
                .ToList();

            result.UniqueActualColumns = actualColumns
                .Except(commonNormalizedColumns, StringComparer.OrdinalIgnoreCase)
                .Select(normalizedName => actualColumnMapping.TryGetValue(normalizedName, out var originalName) ? originalName : normalizedName)
                .ToList();

            // Determine if there are structural differences
            result.HasStructuralDifferences = result.UniqueExpectedColumns.Any() || result.UniqueActualColumns.Any();

            // Set record limit
            int recordLimit = maxRecords ?? _maxRecordsToCompare;
            int rowsToCompare = Math.Min(Math.Min(expected.Count, actual.Count), recordLimit);

            result.TotalRowsCompared = rowsToCompare;
            result.MatchingRows = 0;
            result.DifferentRows = 0;
            result.Differences = new List<DatasetDifference>();

            // If no common columns, datasets are not identical
            if (!commonNormalizedColumns.Any())
            {
                result.IsEquivalent = false;
                result.ComparisonSummary = "No common columns found between datasets";
                return result;
            }
            bool allRowsIdentical = true;

            if (orderIndependent)
            {
                // Create a copy of actual rows that we can remove from as we find matches
                var remainingActualRows = new List<int>(Enumerable.Range(0, Math.Min(actual.Count, recordLimit)));

                

                // For each expected row, try to find a matching actual row
                for (int expectedRowIndex = 0; expectedRowIndex < Math.Min(expected.Count, recordLimit); expectedRowIndex++)
                {
                    bool rowMatched = false;

                    // Try each remaining actual row
                    for (int i = 0; i < remainingActualRows.Count; i++)
                    {
                        int actualRowIndex = remainingActualRows[i];
                        bool currentRowMatches = true;
                        var differences = new List<DatasetDifference>();

                        // Check each common column
                        foreach (var normalizedColumn in commonNormalizedColumns)
                        {
                            var expectedColumnName = expectedColumnMapping[normalizedColumn];
                            var actualColumnName = actualColumnMapping[normalizedColumn];

                            var expectedValue = expected[expectedRowIndex][expectedColumnName];
                            var actualValue = actual[actualRowIndex][actualColumnName];

                            if (!AreValuesExactlyEqual(expectedValue, actualValue))
                            {
                                currentRowMatches = false;
                                // Record differences...
                                break;
                            }
                        }

                        if (currentRowMatches)
                        {
                            // Found a match
                            rowMatched = true;
                            result.MatchingRows++;
                            remainingActualRows.RemoveAt(i);
                            break;
                        }
                    }

                    if (!rowMatched)
                    {
                        // No matching row found
                        allRowsIdentical = false;
                        result.DifferentRows++;
                        // Add to differences...
                    }
                }

                // If there are unmatched actual rows, count them as different
                if (remainingActualRows.Count > 0)
                {
                    allRowsIdentical = false;
                    result.DifferentRows += remainingActualRows.Count;
                }
            }
            else
            {

                // Compare rows using common columns
                allRowsIdentical = true;

                for (int rowIndex = 0; rowIndex < rowsToCompare; rowIndex++)
                {
                    bool rowMatches = true;
                    var rowDifferences = new List<DatasetDifference>();

                    // Check each common column
                    foreach (var normalizedColumn in commonNormalizedColumns)
                    {
                        // Find the actual column names in each dataset that correspond to this normalized name
                        var expectedColumnName = expectedColumnMapping[normalizedColumn];
                        var actualColumnName = actualColumnMapping[normalizedColumn];

                        // Get values from both datasets
                        var expectedValue = expected[rowIndex][expectedColumnName];
                        var actualValue = actual[rowIndex][actualColumnName];

                        // Compare values with exact matching
                        if (!AreValuesExactlyEqual(expectedValue, actualValue))
                        {
                            rowMatches = false;
                            allRowsIdentical = false;

                            // Record the difference
                            rowDifferences.Add(new DatasetDifference
                            {
                                RowIndex = rowIndex,
                                ColumnName = expectedColumnName, // Use original column name
                                ExpectedValue = expectedValue,
                                ActualValue = actualValue,
                                DifferenceType = "ValueMismatch",
                                Description = $"Values differ in row {rowIndex + 1}, column '{expectedColumnName}'"
                            });
                        }
                    }

                    // Update counters based on row comparison
                    if (rowMatches)
                    {
                        result.MatchingRows++;
                    }
                    else
                    {
                        result.DifferentRows++;
                        result.Differences.AddRange(rowDifferences);
                    }
                }
            }

            // Final result determination - binary decision (either 100% or 0% identical)
            result.IsEquivalent = allRowsIdentical;

            // Generate summary
            if (result.IsEquivalent)
            {
                result.ComparisonSummary = $"Datasets are identical. {result.TotalRowsCompared} rows compared with {commonNormalizedColumns.Count} common columns.";
            }
            else
            {
                result.ComparisonSummary = $"Datasets are not identical. {result.DifferentRows} out of {result.TotalRowsCompared} rows have differences.";
            }

            return result;
        }

        /// <summary>
        /// Normalizes a column name for comparison by removing spaces, underscores, and table aliases.
        /// </summary>
        /// <param name="columnName">The column name to normalize.</param>
        /// <returns>Normalized column name for comparison.</returns>
        private string NormalizeColumnName(string columnName)
        {
            if (string.IsNullOrEmpty(columnName))
                return string.Empty;

            // Convert to lowercase
            string normalized = columnName.ToLowerInvariant();

            // Remove table aliases (e.g., "C.Customer_Address" -> "Customer_Address")
            normalized = Regex.Replace(normalized, @"^[a-z0-9_]+\.", "");

            // Remove spaces and underscores (e.g., "Customer_Address" -> "customeraddress")
            normalized = normalized.Replace(" ", "").Replace("_", "");

            return normalized;
        }

        /// <summary>
        /// Determines if two values are exactly equal, with special handling for different types.
        /// </summary>
        /// <param name="value1">First value.</param>
        /// <param name="value2">Second value.</param>
        /// <returns>True if values are exactly equal, false otherwise.</returns>
        private bool AreValuesExactlyEqual(object value1, object value2)
        {
            // Handle null cases
            if (value1 == null && value2 == null)
                return true;

            if (value1 == null || value2 == null)
                return false;

            // Convert both to strings for comparison
            string str1 = FormatValueForComparison(value1);
            string str2 = FormatValueForComparison(value2);

            // Exact string comparison (case sensitive)
            return str1 == str2;
        }

        /// <summary>
        /// Formats a value consistently for comparison.
        /// </summary>
        /// <param name="value">The value to format.</param>
        /// <returns>Formatted string representation.</returns>
        private string FormatValueForComparison(object value)
        {
            if (value == null)
                return "NULL";

            if (value is DateTime dt)
                return dt.ToString("yyyy-MM-dd HH:mm:ss.fff");

            if (value is decimal dec)
                return dec.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture);

            if (value is double dbl)
                return dbl.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture);

            if (value is float flt)
                return flt.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture);

            return value.ToString();
        }
    }
}