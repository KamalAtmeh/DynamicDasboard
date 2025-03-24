using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using DynamicDashboardCommon.Models.TestAutomation;

namespace DynamicDasboardWebAPI.Services.TestAutomation
{
    /// <summary>
    /// Service for comparing datasets without depending on external libraries.
    /// </summary>
    public class DatasetComparisonService
    {
        /// <summary>
        /// Maximum number of records to compare when validating datasets.
        /// </summary>
        private const int MaxRecordsToCompare = 100;

        /// <summary>
        /// Compares two datasets to determine if they are functionally equivalent.
        /// Datasets are considered equivalent if they have the same number of rows and
        /// all common columns have matching values.
        /// </summary>
        /// <param name="expected">The expected dataset</param>
        /// <param name="actual">The actual dataset</param>
        /// <returns>A detailed comparison result</returns>
        public DatasetComparisonResult CompareDatasets(
            List<Dictionary<string, object>> expected,
            List<Dictionary<string, object>> actual)
        {
            var result = new DatasetComparisonResult();

            // Set the original datasets
            result.Expected = expected ?? new List<Dictionary<string, object>>();
            result.Actual = actual ?? new List<Dictionary<string, object>>();

            // Quick check for empty datasets
            if ((expected == null || !expected.Any()) || (actual == null || !actual.Any()))
            {
                result.ComparisonSummary = "One or both datasets are empty";
                result.IsEquivalent = false;
                return result;
            }

            // Check row counts
            if (expected.Count != actual.Count)
            {
                result.ComparisonSummary = $"Row count mismatch: Expected {expected.Count}, Actual {actual.Count}";
                result.IsEquivalent = false;
                return result;
            }

            // Analyze column structures
            AnalyzeColumnStructures(expected, actual, result);

            // Determine how many rows to compare
            int recordsToCompare = Math.Min(Math.Min(expected.Count, actual.Count), MaxRecordsToCompare);
            result.TotalRowsCompared = recordsToCompare;

            // If no common columns, the datasets cannot be functionally equivalent
            if (!result.CommonColumns.Any())
            {
                result.ComparisonSummary = "No common columns found between datasets";
                result.IsEquivalent = false;
                return result;
            }

            // Compare the datasets row by row
            CompareDatasetRows(expected, actual, result, recordsToCompare);

            // Set final result summary
            if (result.IsEquivalent && result.HasStructuralDifferences)
            {
                result.ComparisonSummary = $"Datasets are functionally equivalent despite structural differences. " +
                    $"{result.TotalRowsCompared} rows compared, {result.CommonColumns.Count} common columns.";
            }
            else if (result.IsEquivalent)
            {
                result.ComparisonSummary = $"Datasets are identical. {result.TotalRowsCompared} rows compared.";
            }
            else
            {
                result.ComparisonSummary = $"Datasets differ in {result.DifferentRows} out of {result.TotalRowsCompared} rows compared.";
            }

            return result;
        }

        /// <summary>
        /// Analyzes the column structures of both datasets and identifies common and unique columns.
        /// </summary>
        private void AnalyzeColumnStructures(
            List<Dictionary<string, object>> expected,
            List<Dictionary<string, object>> actual,
            DatasetComparisonResult result)
        {
            // Get columns from both datasets (from the first row)
            var expectedColumns = expected[0].Keys.Select(k => k.ToLowerInvariant()).ToHashSet();
            var actualColumns = actual[0].Keys.Select(k => k.ToLowerInvariant()).ToHashSet();

            // Find common columns (case-insensitive)
            result.CommonColumns = expectedColumns.Intersect(actualColumns).ToList();

            // Find columns unique to each dataset
            result.UniqueExpectedColumns = expectedColumns.Except(actualColumns).ToList();
            result.UniqueActualColumns = actualColumns.Except(expectedColumns).ToList();

            // Determine if there are structural differences
            result.HasStructuralDifferences =
                result.UniqueExpectedColumns.Any() || result.UniqueActualColumns.Any();
        }

        /// <summary>
        /// Compares rows between datasets manually instead of using ObjectsComparer.
        /// </summary>
        private void CompareDatasetRows(
            List<Dictionary<string, object>> expected,
            List<Dictionary<string, object>> actual,
            DatasetComparisonResult result,
            int recordsToCompare)
        {
            try
            {
                // Track overall comparison results
                bool allRowsMatch = true;
                int matchingRows = 0;
                int differentRows = 0;

                // Compare row-by-row
                for (int i = 0; i < recordsToCompare; i++)
                {
                    // Filter the original dictionaries to only include common columns
                    var expectedRow = FilterToCommonColumns(expected[i], result.CommonColumns);
                    var actualRow = FilterToCommonColumns(actual[i], result.CommonColumns);

                    // Compare row values manually
                    var differences = new List<DatasetDifference>();
                    bool rowMatch = true;

                    foreach (var column in result.CommonColumns)
                    {
                        // Get column values (using case-insensitive lookup)
                        var expectedKey = expectedRow.Keys.FirstOrDefault(k =>
                            string.Equals(k, column, StringComparison.OrdinalIgnoreCase));
                        var actualKey = actualRow.Keys.FirstOrDefault(k =>
                            string.Equals(k, column, StringComparison.OrdinalIgnoreCase));

                        // Compare values if keys exist
                        if (expectedKey != null && actualKey != null)
                        {
                            var expectedValue = expectedRow[expectedKey];
                            var actualValue = actualRow[actualKey];

                            // Perform comparison based on value type
                            bool valuesMatch = CompareValues(expectedValue, actualValue);

                            if (!valuesMatch)
                            {
                                rowMatch = false;
                                differences.Add(new DatasetDifference
                                {
                                    RowIndex = i,
                                    ColumnName = column,
                                    ExpectedValue = expectedValue,
                                    ActualValue = actualValue,
                                    DifferenceType = "ValueMismatch",
                                    Description = $"{column}: Expected '{FormatValue(expectedValue)}', Actual '{FormatValue(actualValue)}'"
                                });
                            }
                        }
                    }

                    if (rowMatch)
                    {
                        matchingRows++;
                    }
                    else
                    {
                        differentRows++;
                        allRowsMatch = false;

                        // Add the differences to the result
                        result.Differences.AddRange(differences);
                    }
                }

                // Update result with comparison statistics
                result.MatchingRows = matchingRows;
                result.DifferentRows = differentRows;
                result.IsEquivalent = allRowsMatch;
            }
            catch (Exception ex)
            {
                // Log the error (normally we'd use ILogger here)
                Console.Error.WriteLine($"Error comparing datasets: {ex.Message}");

                // Update result with error information
                result.ComparisonSummary = $"Error during comparison: {ex.Message}";
                result.IsEquivalent = false;
            }
        }

        /// <summary>
        /// Filters a row dictionary to only include specified columns (case-insensitive).
        /// </summary>
        private Dictionary<string, object> FilterToCommonColumns(
            Dictionary<string, object> row,
            List<string> commonColumns)
        {
            var filtered = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            foreach (var column in commonColumns)
            {
                // Find the matching key in the original dictionary (case-insensitive)
                var key = row.Keys.FirstOrDefault(k =>
                    string.Equals(k, column, StringComparison.OrdinalIgnoreCase));

                if (key != null)
                {
                    filtered[key] = row[key];
                }
            }

            return filtered;
        }

        /// <summary>
        /// Compares two values for equality, handling different types.
        /// </summary>
        private bool CompareValues(object value1, object value2)
        {
            // Handle null cases
            if (value1 == null && value2 == null)
                return true;
            if (value1 == null || value2 == null)
                return false;

            // Handle numeric types with tolerance
            if (IsNumeric(value1) && IsNumeric(value2))
            {
                try
                {
                    double d1 = Convert.ToDouble(value1);
                    double d2 = Convert.ToDouble(value2);
                    const double tolerance = 0.0001;
                    return Math.Abs(d1 - d2) < tolerance;
                }
                catch
                {
                    // If conversion fails, fall back to string comparison
                }
            }

            // Handle DateTime comparison
            if ((value1 is DateTime || value2 is DateTime) ||
                (DateTime.TryParse(value1.ToString(), out _) || DateTime.TryParse(value2.ToString(), out _)))
            {
                try
                {
                    DateTime dt1 = value1 is DateTime ? (DateTime)value1 : DateTime.Parse(value1.ToString());
                    DateTime dt2 = value2 is DateTime ? (DateTime)value2 : DateTime.Parse(value2.ToString());

                    // Compare dates without time component
                    return dt1.Date == dt2.Date;
                }
                catch
                {
                    // If parsing fails, fall back to string comparison
                }
            }

            // Default string comparison (case-insensitive)
            return string.Equals(value1.ToString(), value2.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Determines if an object is a numeric type.
        /// </summary>
        private bool IsNumeric(object value)
        {
            if (value == null)
                return false;

            return value is byte || value is sbyte ||
                   value is short || value is ushort ||
                   value is int || value is uint ||
                   value is long || value is ulong ||
                   value is float || value is double ||
                   value is decimal;
        }

        /// <summary>
        /// Formats a value for display.
        /// </summary>
        private string FormatValue(object value)
        {
            if (value == null)
                return "null";

            if (value is DateTime dt)
                return dt.ToString("yyyy-MM-dd");

            if (value is decimal dec)
                return dec.ToString("0.####");

            if (value is double dbl)
                return dbl.ToString("0.####");

            if (value is float flt)
                return flt.ToString("0.####");

            return value.ToString();
        }
    }
}