using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace DynamicDasboardWebAPI.Utilities
{
    /// <summary>
    /// Provides utilities for comparing SQL queries, explanations, and datasets.
    /// </summary>
    public static class ComparisonUtilities
    {
        #region SQL Comparison

        /// <summary>
        /// Calculates similarity between expected and actual SQL queries.
        /// </summary>
        /// <param name="expected">The expected SQL query.</param>
        /// <param name="actual">The actual generated SQL query.</param>
        /// <returns>Similarity score between 0 and 1.</returns>
        public static decimal GetSQLSimilarity(string expected, string actual)
        {
            if (string.IsNullOrWhiteSpace(expected) && string.IsNullOrWhiteSpace(actual))
                return 1.0m;

            if (string.IsNullOrWhiteSpace(expected) || string.IsNullOrWhiteSpace(actual))
                return 0.0m;

            // Normalize SQL queries for better comparison
            var normalizedExpected = NormalizeSQLQuery(expected);
            var normalizedActual = NormalizeSQLQuery(actual);

            // Exact match after normalization
            if (normalizedExpected.Equals(normalizedActual, StringComparison.OrdinalIgnoreCase))
                return 1.0m;

            // Tokenize the SQL for comparison
            var expectedTokens = TokenizeSql(normalizedExpected);
            var actualTokens = TokenizeSql(normalizedActual);

            // Calculate Jaccard similarity for SQL tokens
            decimal jaccardScore = (decimal)CalculateJaccardIndex(expectedTokens.ToHashSet(), actualTokens.ToHashSet());

            // Calculate normalized Levenshtein distance
            decimal levenshteinScore = (decimal)CalculateLevenshteinDistanceNormalized(normalizedExpected, normalizedActual);

            // Weighted combination (70% Jaccard, 30% Levenshtein)
            return (jaccardScore * 0.7m) + (levenshteinScore * 0.3m);
        }

        /// <summary>
        /// Normalizes a SQL query for more accurate comparison.
        /// </summary>
        /// <param name="sql">The SQL query to normalize.</param>
        /// <returns>Normalized SQL query.</returns>
        public static string NormalizeSQLQuery(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
                return string.Empty;

            // Remove comments
            sql = Regex.Replace(sql, @"--.*?$", "", RegexOptions.Multiline);
            sql = Regex.Replace(sql, @"/\*.*?\*/", "", RegexOptions.Singleline);

            // Normalize whitespace
            sql = Regex.Replace(sql, @"\s+", " ").Trim();

            // Normalize quotes
            sql = Regex.Replace(sql, @"['""]([^'""]*)['""]", "'$1'");

            // Normalize case for keywords
            foreach (var keyword in SqlKeywords)
            {
                sql = Regex.Replace(sql, $@"\b{keyword}\b", keyword, RegexOptions.IgnoreCase);
            }

            // Normalize JOIN syntax
            sql = Regex.Replace(sql, @"\bINNER\s+JOIN\b", "JOIN", RegexOptions.IgnoreCase);
            sql = Regex.Replace(sql, @"\bLEFT\s+OUTER\s+JOIN\b", "LEFT JOIN", RegexOptions.IgnoreCase);
            sql = Regex.Replace(sql, @"\bRIGHT\s+OUTER\s+JOIN\b", "RIGHT JOIN", RegexOptions.IgnoreCase);

            // Normalize aliases
            sql = Regex.Replace(sql, @"\bAS\s+([a-zA-Z0-9_]+)\b", "$1", RegexOptions.IgnoreCase);

            return sql;
        }

        /// <summary>
        /// Tokenizes a SQL query into individual components.
        /// </summary>
        /// <param name="sql">The SQL query to tokenize.</param>
        /// <returns>List of SQL tokens.</returns>
        private static List<string> TokenizeSql(string sql)
        {
            // Split SQL by whitespace and punctuation, keeping important SQL constructs together
            var tokens = new List<string>();

            // First, extract quoted strings and replace with placeholders
            var quotes = new List<string>();
            sql = Regex.Replace(sql, @"'([^']*)'", m => {
                quotes.Add(m.Value);
                return $"#QUOTE{quotes.Count - 1}#";
            });

            // Split by SQL clause boundaries
            foreach (var keyword in SqlKeywords)
            {
                sql = Regex.Replace(sql, $@"(\b{keyword}\b)", $" $1 ", RegexOptions.IgnoreCase);
            }

            // Split by common delimiters
            sql = Regex.Replace(sql, @"([,\(\)\[\]=<>!+\-*/%])", " $1 ");

            // Get tokens
            var rawTokens = sql.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                             .Select(t => t.Trim())
                             .Where(t => !string.IsNullOrWhiteSpace(t))
                             .ToList();

            // Restore quoted strings
            for (int i = 0; i < rawTokens.Count; i++)
            {
                if (rawTokens[i].StartsWith("#QUOTE"))
                {
                    int index = int.Parse(rawTokens[i].Substring(6, rawTokens[i].Length - 7));
                    rawTokens[i] = quotes[index];
                }
            }

            return rawTokens;
        }

        /// <summary>
        /// Common SQL keywords for normalization.
        /// </summary>
        private static readonly string[] SqlKeywords = new[]
        {
            "SELECT", "FROM", "WHERE", "JOIN", "LEFT", "RIGHT", "INNER", "OUTER", "CROSS",
            "GROUP", "BY", "HAVING", "ORDER", "LIMIT", "OFFSET", "UNION", "ALL", "CASE",
            "WHEN", "THEN", "ELSE", "END", "IS", "NULL", "NOT", "IN", "EXISTS", "BETWEEN",
            "AND", "OR", "AS", "ON", "WITH"
        };

        #endregion

        #region Explanation Comparison

        /// <summary>
        /// Calculates similarity between expected and actual explanations.
        /// </summary>
        /// <param name="expected">The expected explanation.</param>
        /// <param name="actual">The actual generated explanation.</param>
        /// <returns>Similarity score between 0 and 1.</returns>
        public static decimal GetExplanationSimilarity(string expected, string actual)
        {
            if (string.IsNullOrWhiteSpace(expected) && string.IsNullOrWhiteSpace(actual))
                return 1.0m;

            if (string.IsNullOrWhiteSpace(expected) || string.IsNullOrWhiteSpace(actual))
                return 0.0m;

            // Normalize explanations
            var normalizedExpected = NormalizeExplanation(expected);
            var normalizedActual = NormalizeExplanation(actual);

            // Exact match after normalization
            if (normalizedExpected.Equals(normalizedActual, StringComparison.OrdinalIgnoreCase))
                return 1.0m;

            // Extract key terms
            var expectedTerms = ExtractKeyTerms(normalizedExpected);
            var actualTerms = ExtractKeyTerms(normalizedActual);

            // Calculate Jaccard similarity for terms
            decimal termScore = (decimal)CalculateJaccardIndex(expectedTerms.ToHashSet(), actualTerms.ToHashSet());

            // Calculate normalized Levenshtein distance
            decimal textScore = (decimal)CalculateLevenshteinDistanceNormalized(normalizedExpected, normalizedActual);

            // Weighted combination (60% terms, 40% text)
            return (termScore * 0.6m) + (textScore * 0.4m);
        }

        /// <summary>
        /// Normalizes an explanation for more accurate comparison.
        /// </summary>
        /// <param name="explanation">The explanation to normalize.</param>
        /// <returns>Normalized explanation.</returns>
        private static string NormalizeExplanation(string explanation)
        {
            if (string.IsNullOrWhiteSpace(explanation))
                return string.Empty;

            // Convert to lowercase
            explanation = explanation.ToLowerInvariant();

            // Remove punctuation
            explanation = Regex.Replace(explanation, @"[^\w\s]", " ");

            // Remove stopwords
            foreach (var stopword in StopWords)
            {
                explanation = Regex.Replace(explanation, $@"\b{stopword}\b", " ");
            }

            // Normalize whitespace
            explanation = Regex.Replace(explanation, @"\s+", " ").Trim();

            return explanation;
        }

        /// <summary>
        /// Extracts key terms from an explanation.
        /// </summary>
        /// <param name="explanation">The explanation to extract terms from.</param>
        /// <returns>Set of key terms.</returns>
        private static HashSet<string> ExtractKeyTerms(string explanation)
        {
            if (string.IsNullOrWhiteSpace(explanation))
                return new HashSet<string>();

            // Split by whitespace
            var words = explanation.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            // Filter out very short words and return unique terms
            return words.Where(w => w.Length > 2).ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Common stopwords to filter out during explanation comparison.
        /// </summary>
        private static readonly string[] StopWords = new[]
        {
            "a", "an", "the", "and", "or", "but", "if", "then", "else", "when",
            "to", "of", "in", "on", "at", "by", "for", "with", "about", "against",
            "between", "into", "through", "during", "before", "after", "above", "below",
            "from", "up", "down", "this", "that", "these", "those", "am", "is", "are",
            "was", "were", "be", "been", "being", "have", "has", "had", "do", "does",
            "did", "will", "would", "shall", "should", "can", "could", "may", "might",
            "must", "here", "there", "what", "which", "who", "whom", "whose", "when",
            "where", "why", "how", "all", "any", "both", "each", "few", "more", "most",
            "other", "some", "such", "no", "nor", "not", "only", "own", "same", "so",
            "than", "too", "very", "just", "also"
        };

        #endregion

        #region Dataset Comparison

        /// <summary>
        /// Calculates similarity between expected and actual datasets.
        /// </summary>
        /// <param name="expected">The expected dataset.</param>
        /// <param name="actual">The actual generated dataset.</param>
        /// <returns>Similarity score between 0 and 1, and status description.</returns>
        public static (decimal score, string status) GetDatasetSimilarity(
            List<Dictionary<string, object>> expected,
            List<Dictionary<string, object>> actual)
        {
            // If both null or empty, they're identical
            if ((expected == null || !expected.Any()) && (actual == null || !actual.Any()))
                return (1.0m, "Exact Match (Empty)");

            // If only one is null/empty, they're completely different
            if ((expected == null || !expected.Any()) || (actual == null || !actual.Any()))
                return (0.0m, "Mismatch (One Empty)");

            // Check row counts
            bool rowCountMatch = expected.Count == actual.Count;

            // Structure comparison (columns)
            var expectedColumns = expected[0].Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var actualColumns = actual[0].Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);

            decimal columnSimilarity = (decimal)CalculateJaccardIndex(expectedColumns, actualColumns);
            bool columnsMatch = columnSimilarity > 0.8m;

            // If columns don't match well enough, we'll do a partial comparison
            if (!columnsMatch)
            {
                return (columnSimilarity * 0.5m, "Partial Match (Column Mismatch)");
            }

            // Row content comparison - we compare values regardless of order
            decimal rowSimilarity = CompareRowContent(expected, actual);

            // Generate score based on multiple factors
            decimal finalScore = (columnSimilarity * 0.3m) + (rowSimilarity * 0.7m);

            // Determine status
            string status;
            if (finalScore > 0.95m)
                status = "Exact Match";
            else if (finalScore > 0.8m)
                status = "Close Match";
            else if (rowCountMatch && finalScore > 0.5m)
                status = "Row Count Match (Different Values)";
            else if (finalScore > 0.5m)
                status = "Partial Match";
            else
                status = "Significant Mismatch";

            return (finalScore, status);
        }

        /// <summary>
        /// Compares the content of rows between expected and actual datasets.
        /// </summary>
        /// <param name="expected">The expected dataset.</param>
        /// <param name="actual">The actual generated dataset.</param>
        /// <returns>Similarity score between 0 and 1.</returns>
        private static decimal CompareRowContent(
            List<Dictionary<string, object>> expected,
            List<Dictionary<string, object>> actual)
        {
            // Get common columns for comparison
            var expectedColumns = expected[0].Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var actualColumns = actual[0].Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var commonColumns = expectedColumns.Intersect(actualColumns, StringComparer.OrdinalIgnoreCase).ToList();

            // If no common columns, return 0
            if (!commonColumns.Any())
                return 0m;

            // Convert rows to comparable string representations
            var expectedRows = expected.Select(row => SerializeRowValues(row, commonColumns)).ToHashSet();
            var actualRows = actual.Select(row => SerializeRowValues(row, commonColumns)).ToHashSet();

            // Calculate Jaccard similarity for rows
            return (decimal)CalculateJaccardIndex(expectedRows, actualRows);
        }

        /// <summary>
        /// Serializes a row's values for comparison.
        /// </summary>
        /// <param name="row">The row to serialize.</param>
        /// <param name="columns">The columns to include.</param>
        /// <returns>String representation of the row.</returns>
        private static string SerializeRowValues(Dictionary<string, object> row, List<string> columns)
        {
            var valueBuilder = new StringBuilder();

            foreach (var column in columns)
            {
                string actualColumn = row.Keys.FirstOrDefault(k =>
                    string.Equals(k, column, StringComparison.OrdinalIgnoreCase));

                if (actualColumn != null && row.TryGetValue(actualColumn, out var value))
                {
                    valueBuilder.Append(FormatValueForComparison(value));
                    valueBuilder.Append('|');
                }
                else
                {
                    valueBuilder.Append("NULL|");
                }
            }

            return valueBuilder.ToString();
        }

        /// <summary>
        /// Formats a value for consistent comparison.
        /// </summary>
        /// <param name="value">The value to format.</param>
        /// <returns>Formatted string representation.</returns>
        private static string FormatValueForComparison(object value)
        {
            if (value == null)
                return "NULL";

            if (value is DateTime dateTime)
                return dateTime.ToString("yyyy-MM-dd");

            if (value is decimal decimalValue)
                return decimalValue.ToString("0.00");

            if (value is double doubleValue)
                return doubleValue.ToString("0.00");

            if (value is float floatValue)
                return floatValue.ToString("0.00");

            return value.ToString().ToLowerInvariant();
        }

        /// <summary>
        /// Computes a hash of a dataset for quick comparison.
        /// </summary>
        /// <param name="dataset">The dataset to hash.</param>
        /// <returns>Hash string.</returns>
        public static string ComputeDatasetHash(List<Dictionary<string, object>> dataset)
        {
            if (dataset == null || !dataset.Any())
                return string.Empty;

            // Sort and serialize dataset for consistent hashing
            var sortedData = SortDatasetForHashing(dataset);
            string dataString = JsonSerializer.Serialize(sortedData);

            // Compute SHA256 hash
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(dataString));
                return BitConverter.ToString(hashBytes).Replace("-", "");
            }
        }

        /// <summary>
        /// Prepares a dataset for consistent hashing by sorting and normalizing.
        /// </summary>
        /// <param name="dataset">The dataset to prepare.</param>
        /// <returns>Sorted dataset representation.</returns>
        private static List<Dictionary<string, string>> SortDatasetForHashing(List<Dictionary<string, object>> dataset)
        {
            // Get all column names
            var allColumns = dataset
                .SelectMany(row => row.Keys)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
                .ToList();

            // Normalize and sort the dataset
            return dataset
                .Select(row => allColumns
                    .ToDictionary(
                        column => column,
                        column =>
                        {
                            string actualColumn = row.Keys.FirstOrDefault(k =>
                                string.Equals(k, column, StringComparison.OrdinalIgnoreCase));

                            if (actualColumn != null && row.TryGetValue(actualColumn, out var value))
                                return FormatValueForComparison(value);
                            else
                                return "NULL";
                        },
                        StringComparer.OrdinalIgnoreCase
                    )
                )
                .ToList();
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Calculates the Jaccard index between two sets.
        /// </summary>
        /// <typeparam name="T">The type of set elements.</typeparam>
        /// <param name="set1">The first set.</param>
        /// <param name="set2">The second set.</param>
        /// <returns>Jaccard index between 0 and 1.</returns>
        private static double CalculateJaccardIndex<T>(HashSet<T> set1, HashSet<T> set2)
        {
            if (set1.Count == 0 && set2.Count == 0)
                return 1.0;

            var intersection = set1.Intersect(set2).Count();
            var union = set1.Union(set2).Count();

            return (double)intersection / union;
        }

        /// <summary>
        /// Calculates the normalized Levenshtein distance.
        /// </summary>
        /// <param name="s1">The first string.</param>
        /// <param name="s2">The second string.</param>
        /// <returns>Similarity score between 0 and 1.</returns>
        private static double CalculateLevenshteinDistanceNormalized(string s1, string s2)
        {
            if (string.IsNullOrEmpty(s1) && string.IsNullOrEmpty(s2))
                return 1.0;

            if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2))
                return 0.0;

            int distance = LevenshteinDistance(s1, s2);
            int maxLength = Math.Max(s1.Length, s2.Length);

            return 1.0 - ((double)distance / maxLength);
        }

        /// <summary>
        /// Calculates the Levenshtein distance between two strings.
        /// </summary>
        /// <param name="s1">The first string.</param>
        /// <param name="s2">The second string.</param>
        /// <returns>The Levenshtein distance.</returns>
        private static int LevenshteinDistance(string s1, string s2)
        {
            int n = s1.Length;
            int m = s2.Length;
            int[,] d = new int[n + 1, m + 1];

            // Step 1: Initialize
            if (n == 0) return m;
            if (m == 0) return n;

            for (int i = 0; i <= n; d[i, 0] = i++) { }
            for (int j = 0; j <= m; d[0, j] = j++) { }

            // Step 2: Build distance matrix
            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = (s2[j - 1] == s1[i - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }

            return d[n, m];
        }

        #endregion
    }
}