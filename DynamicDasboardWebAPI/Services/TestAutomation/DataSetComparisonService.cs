using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DynamicDashboardCommon.Models.TestAutomation;
using Microsoft.Extensions.Configuration;

namespace DynamicDasboardWebAPI.Services.TestAutomation
{
    /// <summary>
    /// Enhanced service for comparing datasets with flexible matching criteria.
    /// Supports intelligent column matching, order-independent row comparison,
    /// and configurable comparison limits.
    /// </summary>
    public class DatasetComparisonService
    {
        private readonly IConfiguration _configuration;

        /// <summary>
        /// Maximum number of records to compare when validating datasets.
        /// </summary>
        private readonly int _maxRecordsToCompare;

        /// <summary>
        /// Threshold for column name similarity to consider two columns matching.
        /// </summary>
        private const double ColumnSimilarityThreshold = 0.7;

        /// <summary>
        /// Threshold for row content similarity to consider rows matching.
        /// </summary>
        private const double RowSimilarityThreshold = 0.6;

        /// <summary>
        /// Initializes a new instance of the DatasetComparisonService class.
        /// </summary>
        /// <param name="configuration">Application configuration.</param>
        public DatasetComparisonService(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            // Get configurable comparison limit from settings (default to 100 if not specified)
            _maxRecordsToCompare = _configuration.GetValue<int>("TestAutomation:MaxRecordsToCompare", 100);
        }

        /// <summary>
        /// Compares two datasets with flexible matching criteria.
        /// Datasets are compared based on content similarity regardless of structure.
        /// </summary>
        /// <param name="expected">The expected dataset</param>
        /// <param name="actual">The actual dataset</param>
        /// <param name="maxRecords">Optional override for max records to compare</param>
        /// <returns>A detailed comparison result</returns>
        public DatasetComparisonResult CompareDatasets(
            List<Dictionary<string, object>> expected,
            List<Dictionary<string, object>> actual,
            int? maxRecords = null)
        {
            var result = new DatasetComparisonResult
            {
                Expected = expected ?? new List<Dictionary<string, object>>(),
                Actual = actual ?? new List<Dictionary<string, object>>()
            };

            // Handle empty datasets
            if ((expected == null || !expected.Any()) || (actual == null || !actual.Any()))
            {
                result.ComparisonSummary = "One or both datasets are empty";
                result.IsEquivalent = false;
                return result;
            }

            try
            {
                // Use override value if provided, otherwise use configured value
                int recordLimit = maxRecords ?? _maxRecordsToCompare;

                // Analyze column structures
                AnalyzeColumnStructures(expected, actual, result);

                // Find best column mappings
                var columnMappings = FindBestColumnMappings(expected, actual, result);

                // Calculate rows to compare (minimum of dataset sizes and limit)
                int rowsToCompare = Math.Min(Math.Min(expected.Count, actual.Count), recordLimit);
                result.TotalRowsCompared = rowsToCompare;

                // Compare rows using flexible matching
                CompareRowsFlexibly(expected, actual, columnMappings, result, rowsToCompare);

                // Determine overall equivalence
                DetermineEquivalence(result);

                return result;
            }
            catch (Exception ex)
            {
                // Log error and return result indicating failure
                Console.Error.WriteLine($"Error comparing datasets: {ex.Message}");
                result.ComparisonSummary = $"Error during comparison: {ex.Message}";
                result.IsEquivalent = false;
                return result;
            }
        }

        /// <summary>
        /// Analyzes column structures of both datasets and identifies common and unique columns.
        /// </summary>
        private void AnalyzeColumnStructures(
            List<Dictionary<string, object>> expected,
            List<Dictionary<string, object>> actual,
            DatasetComparisonResult result)
        {
            // Get column names from both datasets (case-insensitive)
            var expectedColumns = expected[0].Keys.Select(k => k.ToLowerInvariant()).ToList();
            var actualColumns = actual[0].Keys.Select(k => k.ToLowerInvariant()).ToList();

            // Find exact match columns (case-insensitive)
            var exactMatches = expectedColumns.Intersect(actualColumns, StringComparer.OrdinalIgnoreCase).ToList();

            // Start with exact matches
            result.CommonColumns = exactMatches;

            // Find columns unique to each dataset
            result.UniqueExpectedColumns = expectedColumns
                .Except(exactMatches, StringComparer.OrdinalIgnoreCase)
                .ToList();

            result.UniqueActualColumns = actualColumns
                .Except(exactMatches, StringComparer.OrdinalIgnoreCase)
                .ToList();

            // Determine if there are structural differences
            result.HasStructuralDifferences = result.UniqueExpectedColumns.Any() || result.UniqueActualColumns.Any();
        }

        /// <summary>
        /// Finds the best mapping between columns in the expected and actual datasets.
        /// Uses multiple strategies including exact matches, synonym matches, and similarity.
        /// </summary>
        private Dictionary<string, string> FindBestColumnMappings(
            List<Dictionary<string, object>> expected,
            List<Dictionary<string, object>> actual,
            DatasetComparisonResult result)
        {
            var columnMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // First add exact matches
            foreach (var commonColumn in result.CommonColumns)
            {
                // Find the actual column name with the same normalized name
                var expectedKey = expected[0].Keys.FirstOrDefault(k =>
                    k.Equals(commonColumn, StringComparison.OrdinalIgnoreCase));

                var actualKey = actual[0].Keys.FirstOrDefault(k =>
                    k.Equals(commonColumn, StringComparison.OrdinalIgnoreCase));

                if (expectedKey != null && actualKey != null)
                {
                    columnMappings[expectedKey] = actualKey;
                }
            }

            // Then try to find matches for unique expected columns based on similarity
            foreach (var expectedColumn in result.UniqueExpectedColumns)
            {
                // Skip if we've already mapped this column
                if (columnMappings.Keys.Any(k => k.Equals(expectedColumn, StringComparison.OrdinalIgnoreCase)))
                    continue;

                var expectedKey = expected[0].Keys.FirstOrDefault(k =>
                    k.Equals(expectedColumn, StringComparison.OrdinalIgnoreCase));

                if (expectedKey == null)
                    continue;

                // Find potential matches among unmapped actual columns
                var bestMatch = FindBestColumnMatch(expectedKey, result.UniqueActualColumns, actual[0]);

                if (bestMatch != null)
                {
                    // Find the actual column name for this normalized name
                    var actualKey = actual[0].Keys.FirstOrDefault(k =>
                        k.Equals(bestMatch, StringComparison.OrdinalIgnoreCase));

                    if (actualKey != null)
                    {
                        columnMappings[expectedKey] = actualKey;

                        // Add to common columns and remove from unique lists
                        if (!result.CommonColumns.Contains(expectedColumn))
                        {
                            result.CommonColumns.Add(expectedColumn);
                        }

                        result.UniqueExpectedColumns.Remove(expectedColumn);
                        result.UniqueActualColumns.Remove(bestMatch);
                    }
                }
            }

            return columnMappings;
        }

        /// <summary>
        /// Finds the best matching column name based on similarity and content patterns.
        /// </summary>
        private string FindBestColumnMatch(
            string expectedColumn,
            List<string> candidateColumns,
            Dictionary<string, object> actualRow)
        {
            // Try to match by name similarity
            var bestMatches = candidateColumns
                .Select(candidate => new
                {
                    Column = candidate,
                    Similarity = CalculateStringSimilarity(
                        NormalizeColumnName(expectedColumn),
                        NormalizeColumnName(candidate))
                })
                .Where(m => m.Similarity >= ColumnSimilarityThreshold)
                .OrderByDescending(m => m.Similarity)
                .ToList();

            if (bestMatches.Any())
            {
                return bestMatches.First().Column;
            }

            // If no matches by name, try to match by synonym patterns
            foreach (var candidate in candidateColumns)
            {
                if (ArePotentialSynonyms(expectedColumn, candidate))
                {
                    return candidate;
                }
            }

            // No good match found
            return null;
        }

        /// <summary>
        /// Compares rows between datasets using flexible matching that ignores order.
        /// </summary>
        private void CompareRowsFlexibly(
            List<Dictionary<string, object>> expected,
            List<Dictionary<string, object>> actual,
            Dictionary<string, string> columnMappings,
            DatasetComparisonResult result,
            int rowsToCompare)
        {
            // Initialize matching counters
            result.MatchingRows = 0;
            result.DifferentRows = 0;
            result.Differences.Clear();

            // Extract mapped columns from each dataset
            var expectedRows = expected.Take(rowsToCompare)
                .Select(row => ExtractMappedValues(row, columnMappings.Keys.ToList()))
                .ToList();

            var actualRows = actual.Take(rowsToCompare)
                .Select(row => ExtractMappedValues(row, columnMappings.Values.ToList()))
                .ToList();

            // Create a copy of rows we can remove from as we find matches
            var remainingActualRows = new List<Dictionary<string, object>>(actualRows);

            // For each expected row, find the best matching actual row
            for (int expectedIndex = 0; expectedIndex < expectedRows.Count; expectedIndex++)
            {
                var expectedRow = expectedRows[expectedIndex];
                bool rowMatched = false;

                // Find best matching row
                var bestMatch = FindBestRowMatch(expectedRow, remainingActualRows, columnMappings);

                if (bestMatch.Row != null)
                {
                    // We found a match - remove it from remaining rows
                    remainingActualRows.Remove(bestMatch.Row);

                    // Check if there are differences within this match
                    var rowDifferences = CompareRowValues(
                        expectedRow,
                        bestMatch.Row,
                        columnMappings,
                        expectedIndex,
                        bestMatch.Index);

                    if (!rowDifferences.Any())
                    {
                        // Perfect match
                        result.MatchingRows++;
                        rowMatched = true;
                    }
                    else
                    {
                        // Matched row but with differences
                        result.DifferentRows++;
                        result.Differences.AddRange(rowDifferences);
                    }
                }
                else
                {
                    // No match found for this row
                    result.DifferentRows++;

                    // Add difference showing the expected row has no match
                    foreach (var column in expectedRow.Keys)
                    {
                        result.Differences.Add(new DatasetDifference
                        {
                            RowIndex = expectedIndex,
                            ColumnName = column,
                            ExpectedValue = expectedRow[column],
                            ActualValue = null,
                            DifferenceType = "RowNotFound",
                            Description = $"Expected row {expectedIndex + 1} not found in actual dataset"
                        });
                    }
                }
            }
        }

        /// <summary>
        /// Extracts values for mapped columns from a row.
        /// </summary>
        private Dictionary<string, object> ExtractMappedValues(
            Dictionary<string, object> row,
            List<string> columns)
        {
            var result = new Dictionary<string, object>();

            foreach (var column in columns)
            {
                var key = row.Keys.FirstOrDefault(k =>
                    string.Equals(k, column, StringComparison.OrdinalIgnoreCase));

                if (key != null && row.TryGetValue(key, out var value))
                {
                    result[column] = value;
                }
            }

            return result;
        }

        /// <summary>
        /// Finds the best matching row in a set of rows based on content similarity.
        /// </summary>
        private (Dictionary<string, object> Row, int Index) FindBestRowMatch(
            Dictionary<string, object> expectedRow,
            List<Dictionary<string, object>> candidateRows,
            Dictionary<string, string> columnMappings)
        {
            if (!candidateRows.Any())
                return (null, -1);

            // Calculate similarity scores for all candidates
            var scoredRows = candidateRows
                .Select((row, index) => new
                {
                    Row = row,
                    Index = index,
                    Score = CalculateRowSimilarity(expectedRow, row, columnMappings)
                })
                .OrderByDescending(sr => sr.Score)
                .ToList();

            // Return the best match if it meets the threshold
            var bestMatch = scoredRows.First();
            if (bestMatch.Score >= RowSimilarityThreshold)
            {
                return (bestMatch.Row, bestMatch.Index);
            }

            return (null, -1);
        }

        /// <summary>
        /// Calculates similarity between two rows based on their common columns.
        /// </summary>
        private double CalculateRowSimilarity(
            Dictionary<string, object> expectedRow,
            Dictionary<string, object> actualRow,
            Dictionary<string, string> columnMappings)
        {
            int matchCount = 0;
            int totalComparisons = 0;

            // Compare each mapped column
            foreach (var mapping in columnMappings)
            {
                // Get expected and actual values
                if (expectedRow.TryGetValue(mapping.Key, out var expectedValue))
                {
                    if (actualRow.TryGetValue(mapping.Value, out var actualValue))
                    {
                        totalComparisons++;

                        if (CompareValues(expectedValue, actualValue))
                        {
                            matchCount++;
                        }
                    }
                }
            }

            // Calculate similarity ratio
            return totalComparisons > 0
                ? (double)matchCount / totalComparisons
                : 0;
        }

        /// <summary>
        /// Compares individual values from two rows and returns differences.
        /// </summary>
        private List<DatasetDifference> CompareRowValues(
            Dictionary<string, object> expectedRow,
            Dictionary<string, object> actualRow,
            Dictionary<string, string> columnMappings,
            int expectedIndex,
            int actualIndex)
        {
            var differences = new List<DatasetDifference>();

            // Compare each mapped column
            foreach (var mapping in columnMappings)
            {
                // Get expected and actual values
                if (expectedRow.TryGetValue(mapping.Key, out var expectedValue))
                {
                    if (actualRow.TryGetValue(mapping.Value, out var actualValue))
                    {
                        if (!CompareValues(expectedValue, actualValue))
                        {
                            // Values don't match - record the difference
                            differences.Add(new DatasetDifference
                            {
                                RowIndex = expectedIndex,
                                ColumnName = mapping.Key,
                                ExpectedValue = expectedValue,
                                ActualValue = actualValue,
                                DifferenceType = "ValueMismatch",
                                Description = $"Column '{mapping.Key}': Expected '{FormatValue(expectedValue)}', Actual '{FormatValue(actualValue)}'"
                            });
                        }
                    }
                    else
                    {
                        // Column exists in expected but not in actual row
                        differences.Add(new DatasetDifference
                        {
                            RowIndex = expectedIndex,
                            ColumnName = mapping.Key,
                            ExpectedValue = expectedValue,
                            ActualValue = null,
                            DifferenceType = "ColumnMissing",
                            Description = $"Column '{mapping.Key}' missing in actual row"
                        });
                    }
                }
            }

            return differences;
        }

        /// <summary>
        /// Determines overall equivalence based on comparison results.
        /// </summary>
        private void DetermineEquivalence(DatasetComparisonResult result)
        {
            // Datasets are equivalent if there are no different rows
            result.IsEquivalent = result.DifferentRows == 0;

            // Generate appropriate summary message
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
        }

        #region Helper Methods

        /// <summary>
        /// Compares two values for equality with type flexibility.
        /// </summary>
        private bool CompareValues(object value1, object value2)
        {
            // Handle null cases
            if (value1 == null && value2 == null)
                return true;

            if (value1 == null || value2 == null)
                return false;

            // Convert both to strings for comparison
            string str1 = FormatValue(value1);
            string str2 = FormatValue(value2);

            // Special handling for numeric values
            if (IsNumeric(value1) && IsNumeric(value2))
            {
                try
                {
                    decimal num1 = Convert.ToDecimal(value1);
                    decimal num2 = Convert.ToDecimal(value2);

                    // Consider numbers equal if they're within a small tolerance
                    const decimal tolerance = 0.0001m;
                    return Math.Abs(num1 - num2) < tolerance;
                }
                catch
                {
                    // If conversion fails, fall back to string comparison
                }
            }

            // Special handling for dates
            if ((value1 is DateTime || value2 is DateTime) &&
                DateTime.TryParse(str1, out var date1) &&
                DateTime.TryParse(str2, out var date2))
            {
                // First try exact comparison (including time if available)
                if (date1 == date2)
                    return true;

                // Then try date-only comparison (ignoring time)
                if (date1.Date == date2.Date)
                    return true;

                // For dates with different timezones, compare using UTC time
                return date1.ToUniversalTime().Date == date2.ToUniversalTime().Date;
            }

            // Default case: case-insensitive string comparison
            return string.Equals(str1, str2, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Determines if an object represents a numeric value.
        /// </summary>
        private bool IsNumeric(object value)
        {
            return value is byte || value is sbyte ||
                   value is short || value is ushort ||
                   value is int || value is uint ||
                   value is long || value is ulong ||
                   value is float || value is double ||
                   value is decimal;
        }

        /// <summary>
        /// Formats a value consistently for display and comparison.
        /// Uses culture-invariant formatting to ensure consistency across environments.
        /// </summary>
        private string FormatValue(object value)
        {
            if (value == null)
                return "null";

            if (value is DateTime dt)
                // Use ISO 8601 format with InvariantCulture for environment-independent formatting
                return dt.ToString("o", System.Globalization.CultureInfo.InvariantCulture);

            if (value is decimal dec)
                return dec.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture);

            if (value is double dbl)
                return dbl.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture);

            if (value is float flt)
                return flt.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture);

            return value.ToString();
        }

        /// <summary>
        /// Normalizes a column name for comparison by removing common prefixes/suffixes,
        /// spaces, and non-alphanumeric characters.
        /// </summary>
        private string NormalizeColumnName(string columnName)
        {
            if (string.IsNullOrEmpty(columnName))
                return string.Empty;

            // Convert to lowercase
            string normalized = columnName.ToLowerInvariant();

            // Remove common prefixes
            string[] prefixes = { "col_", "column_", "fld_", "field_" };
            foreach (var prefix in prefixes)
            {
                if (normalized.StartsWith(prefix))
                {
                    normalized = normalized.Substring(prefix.Length);
                    break;
                }
            }

            // Remove common suffixes
            string[] suffixes = { "_id", "_code", "_name", "_value" };
            foreach (var suffix in suffixes)
            {
                if (normalized.EndsWith(suffix))
                {
                    normalized = normalized.Substring(0, normalized.Length - suffix.Length);
                    break;
                }
            }

            // Remove non-alphanumeric characters
            normalized = Regex.Replace(normalized, @"[^a-z0-9]", "");

            return normalized;
        }

        /// <summary>
        /// Calculates string similarity using a combination of character-based and pattern-based matching.
        /// </summary>
        private double CalculateStringSimilarity(string s1, string s2)
        {
            if (string.IsNullOrEmpty(s1) && string.IsNullOrEmpty(s2))
                return 1.0;

            if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2))
                return 0.0;

            // Calculate Levenshtein distance
            int distance = LevenshteinDistance(s1, s2);
            int maxLength = Math.Max(s1.Length, s2.Length);

            return 1.0 - ((double)distance / maxLength);
        }

        /// <summary>
        /// Calculates Levenshtein distance between two strings.
        /// </summary>
        private int LevenshteinDistance(string s1, string s2)
        {
            int[,] d = new int[s1.Length + 1, s2.Length + 1];

            for (int i = 0; i <= s1.Length; i++)
                d[i, 0] = i;

            for (int j = 0; j <= s2.Length; j++)
                d[0, j] = j;

            for (int j = 1; j <= s2.Length; j++)
            {
                for (int i = 1; i <= s1.Length; i++)
                {
                    int cost = (s1[i - 1] == s2[j - 1]) ? 0 : 1;

                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }

            return d[s1.Length, s2.Length];
        }

        /// <summary>
        /// Determines if two column names are potential synonyms based on common naming patterns.
        /// </summary>
        private bool ArePotentialSynonyms(string column1, string column2)
        {
            // Common synonym patterns
            var synonymPatterns = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
            {
                { "id", new List<string> { "key", "code", "no", "number", "identifier" } },
                { "name", new List<string> { "title", "label", "description", "desc" } },
                { "date", new List<string> { "time", "timestamp", "datetime" } },
                { "phone", new List<string> { "telephone", "mobile", "cell", "contact" } },
                { "address", new List<string> { "location", "street", "place" } },
                { "email", new List<string> { "mail", "emailaddress", "e-mail" } },
                { "status", new List<string> { "state", "condition", "flag" } },
                { "sex", new List<string> { "gender" } }
            };

            string norm1 = NormalizeColumnName(column1);
            string norm2 = NormalizeColumnName(column2);

            // Check each pattern
            foreach (var pattern in synonymPatterns)
            {
                bool pattern1MatchesKey = norm1.Contains(pattern.Key);
                bool pattern2MatchesKey = norm2.Contains(pattern.Key);

                // Check if one matches the key and the other matches a synonym
                if (pattern1MatchesKey && !pattern2MatchesKey)
                {
                    if (pattern.Value.Any(synonym => norm2.Contains(synonym)))
                        return true;
                }
                else if (!pattern1MatchesKey && pattern2MatchesKey)
                {
                    if (pattern.Value.Any(synonym => norm1.Contains(synonym)))
                        return true;
                }
            }

            return false;
        }

        #endregion
    }
}