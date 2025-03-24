using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using DynamicDashboardCommon.Models.TestAutomation;
using ObjectsComparer;

namespace DynamicDasboardWebAPI.Services.TestAutomation
{
    /// <summary>
    /// Service for comparing datasets using ObjectsComparer library.
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

            // Compare the datasets using ObjectsComparer
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
        /// Compares rows between datasets using ObjectsComparer.
        /// </summary>
        private void CompareDatasetRows(
            List<Dictionary<string, object>> expected,
            List<Dictionary<string, object>> actual,
            DatasetComparisonResult result,
            int recordsToCompare)
        {
            try
            {
                // Create the comparer configuration
                var settings = new ComparisonSettings
                {
                    KeyComparer = StringComparer.OrdinalIgnoreCase,
                    EmptyAndNullEnumerablesEqual = true
                };

                // Handle value comparisons for different data types
                settings.ValueComparers.Add(typeof(string), new CaseInsensitiveStringComparer());
                settings.ValueComparers.Add(typeof(DateTime), new DateTimeComparer());
                settings.ValueComparers.Add(typeof(double), new NumericComparer<double>());
                settings.ValueComparers.Add(typeof(decimal), new NumericComparer<decimal>());
                settings.ValueComparers.Add(typeof(float), new NumericComparer<float>());

                // Create and configure the comparer
                var comparer = new Comparer<Dictionary<string, object>>(settings);

                // We only care about comparing common columns
                // Add a member filter to ignore columns that don't exist in both datasets
                if (result.HasStructuralDifferences)
                {
                    comparer.Config.MembersToInclude = result.CommonColumns
                        .Select(col => new MemberInfo(col))
                        .ToList();
                }

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

                    // Compare the current row
                    var rowMatch = comparer.Compare(expectedRow, actualRow, out var differences);

                    if (rowMatch)
                    {
                        matchingRows++;
                    }
                    else
                    {
                        differentRows++;
                        allRowsMatch = false;

                        // Record the differences
                        foreach (var diff in differences)
                        {
                            result.Differences.Add(new DatasetDifference
                            {
                                RowIndex = i,
                                ColumnName = diff.MemberPath,
                                ExpectedValue = diff.Value1,
                                ActualValue = diff.Value2,
                                DifferenceType = diff.DifferenceType.ToString(),
                                Description = $"{diff.MemberPath}: Expected '{diff.Value1}', Actual '{diff.Value2}'"
                            });
                        }
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

        #region Custom Comparers

        /// <summary>
        /// Case-insensitive string comparer for ObjectsComparer.
        /// </summary>
        private class CaseInsensitiveStringComparer : IValueComparer
        {
            public bool Compare(object value1, object value2, ComparisonSettings settings, out string errorMessage)
            {
                errorMessage = null;

                // Handle null cases
                if (value1 == null && value2 == null)
                    return true;
                if (value1 == null || value2 == null)
                {
                    errorMessage = $"One value is null: '{value1}' vs '{value2}'";
                    return false;
                }

                // Compare strings case-insensitively
                string s1 = value1.ToString();
                string s2 = value2.ToString();
                bool isEqual = string.Equals(s1, s2, StringComparison.OrdinalIgnoreCase);

                if (!isEqual)
                {
                    errorMessage = $"String values differ: '{s1}' vs '{s2}'";
                }

                return isEqual;
            }
        }

        /// <summary>
        /// Date comparer that ignores time portion for ObjectsComparer.
        /// </summary>
        private class DateTimeComparer : IValueComparer
        {
            public bool Compare(object value1, object value2, ComparisonSettings settings, out string errorMessage)
            {
                errorMessage = null;

                // Handle null cases
                if (value1 == null && value2 == null)
                    return true;
                if (value1 == null || value2 == null)
                {
                    errorMessage = $"One value is null: '{value1}' vs '{value2}'";
                    return false;
                }

                // Try to parse as dates if they're not already DateTime objects
                DateTime date1, date2;

                if (value1 is DateTime dt1)
                {
                    date1 = dt1;
                }
                else if (!DateTime.TryParse(value1.ToString(), out date1))
                {
                    errorMessage = $"Could not parse '{value1}' as a valid date";
                    return false;
                }

                if (value2 is DateTime dt2)
                {
                    date2 = dt2;
                }
                else if (!DateTime.TryParse(value2.ToString(), out date2))
                {
                    errorMessage = $"Could not parse '{value2}' as a valid date";
                    return false;
                }

                // Compare dates
                bool isEqual = date1.Date == date2.Date;

                if (!isEqual)
                {
                    errorMessage = $"Dates differ: '{date1:yyyy-MM-dd}' vs '{date2:yyyy-MM-dd}'";
                }

                return isEqual;
            }
        }

        /// <summary>
        /// Generic numeric comparer with tolerance for floating-point values.
        /// </summary>
        private class NumericComparer<T> : IValueComparer where T : IComparable
        {
            private readonly double _tolerance = 0.0001;

            public bool Compare(object value1, object value2, ComparisonSettings settings, out string errorMessage)
            {
                errorMessage = null;

                // Handle null cases
                if (value1 == null && value2 == null)
                    return true;
                if (value1 == null || value2 == null)
                {
                    errorMessage = $"One value is null: '{value1}' vs '{value2}'";
                    return false;
                }

                try
                {
                    // Convert to double for comparison with tolerance
                    double d1 = Convert.ToDouble(value1);
                    double d2 = Convert.ToDouble(value2);

                    // Compare with tolerance
                    bool isEqual = Math.Abs(d1 - d2) < _tolerance;

                    if (!isEqual)
                    {
                        errorMessage = $"Numeric values differ: '{d1}' vs '{d2}'";
                    }

                    return isEqual;
                }
                catch (Exception ex)
                {
                    errorMessage = $"Failed to compare numeric values: {ex.Message}";
                    return false;
                }
            }
        }

        #endregion
    }
}