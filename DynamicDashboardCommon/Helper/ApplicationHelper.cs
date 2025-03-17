using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Diagnostics;
using DynamicDashboardCommon.Models;
using System.Text.RegularExpressions;


namespace DynamicDashboardCommon.Helper
{
    public static class ApplicationHelper
    {

        // Validates whether the input string is valid JSON.
        public static bool IsValidJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return false;

            try
            {
                JsonDocument.Parse(json);
                return true;
            }
            catch
            {
                return false;
            }
        }

        // Deserializes JSON into an object of type T.
        public static T Deserialize<T>(string json)
        {
            if (IsValidJson(json))
                return JsonSerializer.Deserialize<T>(json);
            else
                throw new ArgumentException("Invalid JSON");
        }

        // Serializes an object to an indented JSON string.
        public static string Serialize(object obj)
        {
            return JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true });
        }

        // Returns the details of an exception as a string.
        public static string GetExceptionDetails(Exception ex)
        {
            if (ex == null) return string.Empty;

            // Create a stack trace with file info
            var stackTrace = new StackTrace(ex, true);
            // Get the first frame (where exception originated)
            var frame = stackTrace.GetFrame(0);
            var fileName = frame?.GetFileName() ?? "Unknown File";
            var methodName = frame?.GetMethod()?.Name ?? "Unknown Method";

            var sb = new StringBuilder();
            sb.AppendLine("===== Exception Details =====");
            sb.AppendLine($"File: {fileName}");
            sb.AppendLine($"Method: {methodName}");
            sb.AppendLine($"Message: {ex.Message}");
            sb.AppendLine("Full Stack Trace:");
            sb.AppendLine(ex.StackTrace);
            sb.AppendLine("=============================");

            return sb.ToString();

        }


        /// <summary>
        /// Utility class for SQL script validation against database schema
        /// </summary>
        public static class SqlValidationHelper
        {
            private static readonly Regex TableRegex = new Regex(@"(?:FROM|JOIN|UPDATE|INTO)\s+(?:(?<schema>\w+)\.)?(?<table>\w+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            private static readonly Regex ColumnRegex = new Regex(@"(?:SELECT|WHERE|ORDER\s+BY|GROUP\s+BY|HAVING|SET)\s+.*?(?:(?<table>\w+)\.)?(?<column>\w+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            private static readonly Regex JoinConditionRegex = new Regex(@"(?<table1>\w+)\.(?<column1>\w+)\s*=\s*(?<table2>\w+)\.(?<column2>\w+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

            /// <summary>
            /// Basic syntactic validation of SQL script
            /// </summary>
            /// <param name="sqlScript">SQL script to validate</param>
            /// <returns>Validation result with error details if any</returns>
            public static QueryValidationResult ValidateSqlSyntax(string sqlScript)
            {
                if (string.IsNullOrWhiteSpace(sqlScript))
                {
                    return new QueryValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "SQL script cannot be empty"
                    };
                }

                // Basic syntax validation
                try
                {
                    // Check for balanced parentheses
                    if (!HasBalancedParentheses(sqlScript))
                    {
                        return new QueryValidationResult
                        {
                            IsValid = false,
                            ErrorMessage = "SQL has unbalanced parentheses"
                        };
                    }

                    // Check for incomplete statements (ending with semicolons)
                    if (!sqlScript.TrimEnd().EndsWith(";") && !IsBasicStatementComplete(sqlScript))
                    {
                        return new QueryValidationResult
                        {
                            IsValid = false,
                            ErrorMessage = "SQL statement appears incomplete"
                        };
                    }

                    // Check for basic required clauses in SELECT statements
                    if (sqlScript.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase) &&
                       !sqlScript.Contains("FROM", StringComparison.OrdinalIgnoreCase))
                    {
                        return new QueryValidationResult
                        {
                            IsValid = false,
                            ErrorMessage = "SELECT statement missing FROM clause"
                        };
                    }

                    return new QueryValidationResult
                    {
                        IsValid = true
                    };
                }
                catch (Exception ex)
                {
                    return new QueryValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = $"SQL syntax validation error: {ex.Message}"
                    };
                }
            }

            /// <summary>
            /// Validates SQL script against a database schema to ensure all referenced objects exist
            /// </summary>
            /// <param name="sqlScript">SQL script to validate</param>
            /// <param name="schema">Database schema containing tables, columns and relationships</param>
            /// <param name="validateRelations">Whether to validate relationship constraints</param>
            /// <returns>Validation result with error details if any</returns>
            public static QueryValidationResult ValidateSqlAgainstSchema(string sqlScript, DatabaseSchema schema, bool validateRelations = false)
            {
                // First validate basic syntax
                var syntaxResult = ValidateSqlSyntax(sqlScript);
                if (!syntaxResult.IsValid)
                {
                    return syntaxResult;
                }

                // Extract referenced objects
                var referencedObjects = ExtractReferencedObjects(sqlScript);

                // Validate tables
                var tableValidationResult = ValidateTables(referencedObjects.Tables, schema);
                if (!tableValidationResult.IsValid)
                {
                    return tableValidationResult;
                }

                // Validate columns
                var columnValidationResult = ValidateColumns(referencedObjects.TableColumns, schema);
                if (!columnValidationResult.IsValid)
                {
                    return columnValidationResult;
                }

                // Validate relations if required
                if (validateRelations && referencedObjects.Relations.Count > 0)
                {
                    var relationValidationResult = ValidateRelations(referencedObjects.Relations, schema);
                    if (!relationValidationResult.IsValid)
                    {
                        return relationValidationResult;
                    }
                }

                return new QueryValidationResult
                {
                    IsValid = true,
                    ReferencedObjects = referencedObjects
                };
            }

            /// <summary>
            /// Extracts database objects referenced in the SQL script
            /// </summary>
            /// <param name="sqlScript">SQL script to analyze</param>
            /// <returns>Collection of referenced database objects</returns>
            public static QueryReferencedObjects ExtractReferencedObjects(string sqlScript)
            {
                var result = new QueryReferencedObjects
                {
                    Tables = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                    TableColumns = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase),
                    Relations = new List<(string SourceTable, string SourceColumn, string TargetTable, string TargetColumn)>()
                };

                // Extract tables
                var tableMatches = TableRegex.Matches(sqlScript);
                foreach (Match match in tableMatches)
                {
                    var table = match.Groups["table"].Value;
                    result.Tables.Add(table);

                    // Initialize column collection for this table
                    if (!result.TableColumns.ContainsKey(table))
                    {
                        result.TableColumns[table] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    }
                }

                // Extract columns
                var columnMatches = ColumnRegex.Matches(sqlScript);
                foreach (Match match in columnMatches)
                {
                    var column = match.Groups["column"].Value;
                    var table = match.Groups["table"].Success ? match.Groups["table"].Value : null;

                    if (!string.IsNullOrEmpty(table) && result.Tables.Contains(table))
                    {
                        result.TableColumns[table].Add(column);
                    }
                }

                // Extract join conditions (potential relations)
                var joinMatches = JoinConditionRegex.Matches(sqlScript);
                foreach (Match match in joinMatches)
                {
                    var table1 = match.Groups["table1"].Value;
                    var column1 = match.Groups["column1"].Value;
                    var table2 = match.Groups["table2"].Value;
                    var column2 = match.Groups["column2"].Value;

                    result.Relations.Add((table1, column1, table2, column2));
                }

                return result;
            }

            private static QueryValidationResult ValidateTables(HashSet<string> tables, DatabaseSchema schema)
            {
                if (schema.Tables == null || schema.Tables.Count == 0)
                {
                    return new QueryValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "Schema does not contain any tables"
                    };
                }

                var schemaTables = schema.Tables.Select(t => t.DBName).ToHashSet(StringComparer.OrdinalIgnoreCase);
                var missingTables = tables.Where(t => !schemaTables.Contains(t)).ToList();

                if (missingTables.Count > 0)
                {
                    return new QueryValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = $"Tables not found in schema: {string.Join(", ", missingTables)}"
                    };
                }

                return new QueryValidationResult { IsValid = true };
            }

            private static QueryValidationResult ValidateColumns(Dictionary<string, HashSet<string>> tableColumns, DatabaseSchema schema)
            {
                if (schema.Tables == null || schema.Tables.Count == 0)
                {
                    return new QueryValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "Schema does not contain any tables"
                    };
                }

                foreach (var tableColumn in tableColumns)
                {
                    var table = schema.Tables.FirstOrDefault(t =>
                        string.Equals(t.DBName, tableColumn.Key, StringComparison.OrdinalIgnoreCase));

                    if (table == null)
                    {
                        return new QueryValidationResult
                        {
                            IsValid = false,
                            ErrorMessage = $"Table not found in schema: {tableColumn.Key}"
                        };
                    }

                    var schemaColumns = table.Columns.Select(c => c.DBName).ToHashSet(StringComparer.OrdinalIgnoreCase);
                    var missingColumns = tableColumn.Value.Where(c => !schemaColumns.Contains(c)).ToList();

                    if (missingColumns.Count > 0)
                    {
                        return new QueryValidationResult
                        {
                            IsValid = false,
                            ErrorMessage = $"Columns not found in table '{tableColumn.Key}': {string.Join(", ", missingColumns)}"
                        };
                    }
                }

                return new QueryValidationResult { IsValid = true };
            }

            private static QueryValidationResult ValidateRelations(List<(string SourceTable, string SourceColumn, string TargetTable, string TargetColumn)> relations, DatabaseSchema schema)
            {
                if (schema.Relationships == null || schema.Relationships.Count == 0)
                {
                    return new QueryValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "Schema does not contain any relationships"
                    };
                }

                foreach (var relation in relations)
                {
                    // Find source and target tables in schema
                    var sourceTable = schema.Tables.FirstOrDefault(t =>
                        string.Equals(t.DBName, relation.SourceTable, StringComparison.OrdinalIgnoreCase));
                    var targetTable = schema.Tables.FirstOrDefault(t =>
                        string.Equals(t.DBName, relation.TargetTable, StringComparison.OrdinalIgnoreCase));

                    if (sourceTable == null || targetTable == null)
                    {
                        return new QueryValidationResult
                        {
                            IsValid = false,
                            ErrorMessage = $"Source or target table not found for relation: {relation.SourceTable}.{relation.SourceColumn} = {relation.TargetTable}.{relation.TargetColumn}"
                        };
                    }

                    // Find corresponding relationship in schema
                    bool relationFound = false;
                    foreach (var schemaRelation in schema.Relationships)
                    {
                        var sourceTableDb = schema.Tables.FirstOrDefault(t => t.ID == schemaRelation.Source.TableID)?.DBName;
                        var targetTableDb = schema.Tables.FirstOrDefault(t => t.ID == schemaRelation.Target.TableID)?.DBName;

                        if (string.IsNullOrEmpty(sourceTableDb) || string.IsNullOrEmpty(targetTableDb))
                            continue;

                        var sourceColumnDb = sourceTable.Columns.FirstOrDefault(c => c.ID == schemaRelation.Source.ColumnID)?.DBName;
                        var targetColumnDb = targetTable.Columns.FirstOrDefault(c => c.ID == schemaRelation.Target.ColumnID)?.DBName;

                        if (string.IsNullOrEmpty(sourceColumnDb) || string.IsNullOrEmpty(targetColumnDb))
                            continue;

                        if ((string.Equals(sourceTableDb, relation.SourceTable, StringComparison.OrdinalIgnoreCase) &&
                             string.Equals(sourceColumnDb, relation.SourceColumn, StringComparison.OrdinalIgnoreCase) &&
                             string.Equals(targetTableDb, relation.TargetTable, StringComparison.OrdinalIgnoreCase) &&
                             string.Equals(targetColumnDb, relation.TargetColumn, StringComparison.OrdinalIgnoreCase))
                            ||
                            (string.Equals(sourceTableDb, relation.TargetTable, StringComparison.OrdinalIgnoreCase) &&
                             string.Equals(sourceColumnDb, relation.TargetColumn, StringComparison.OrdinalIgnoreCase) &&
                             string.Equals(targetTableDb, relation.SourceTable, StringComparison.OrdinalIgnoreCase) &&
                             string.Equals(targetColumnDb, relation.SourceColumn, StringComparison.OrdinalIgnoreCase)))
                        {
                            relationFound = true;
                            break;
                        }
                    }

                    if (!relationFound)
                    {
                        return new QueryValidationResult
                        {
                            IsValid = false,
                            ErrorMessage = $"Relationship not found in schema: {relation.SourceTable}.{relation.SourceColumn} = {relation.TargetTable}.{relation.TargetColumn}"
                        };
                    }
                }

                return new QueryValidationResult { IsValid = true };
            }

            private static bool HasBalancedParentheses(string sql)
            {
                int openCount = 0;

                foreach (char c in sql)
                {
                    if (c == '(')
                        openCount++;
                    else if (c == ')')
                        openCount--;

                    if (openCount < 0)
                        return false;
                }

                return openCount == 0;
            }

            private static bool IsBasicStatementComplete(string sql)
            {
                // Check if SQL contains basic required keywords for common statements
                string normalizedSql = sql.ToUpperInvariant();

                if (normalizedSql.Contains("SELECT"))
                    return normalizedSql.Contains("FROM");

                if (normalizedSql.Contains("UPDATE"))
                    return normalizedSql.Contains("SET");

                if (normalizedSql.Contains("INSERT"))
                    return normalizedSql.Contains("VALUES") || normalizedSql.Contains("SELECT");

                if (normalizedSql.Contains("DELETE"))
                    return normalizedSql.Contains("FROM");

                return true; // Other statement types or can't determine
            }


        }

        /// <summary>
        /// Result of SQL validation operation
        /// </summary>

    }
}
