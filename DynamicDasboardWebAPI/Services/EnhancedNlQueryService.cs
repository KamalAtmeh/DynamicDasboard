using DynamicDashboardCommon.Models;
using DynamicDasboardWebAPI.Repositories;
using DynamicDasboardWebAPI.Services.LLM;
using DynamicDasboardWebAPI.Utilities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DynamicDashboardCommon.Models.LLM;
using DynamicDashboardCommon.Models.DynamicDashboardCommon.Models;

namespace DynamicDasboardWebAPI.Services
{
    /// <summary>
    /// Enhanced service for processing natural language queries with a two-step confirmation process
    /// </summary>
    public class EnhancedNlQueryService
    {
        private readonly NlQueryRepository _repository;
        private readonly LLMServiceFactory _llmServiceFactory;
        private readonly ILLMService _llmService;
        private readonly ILogger<EnhancedNlQueryService> _logger;

        public EnhancedNlQueryService(
            NlQueryRepository repository,
            LLMServiceFactory llmServiceFactory,
            ILogger<EnhancedNlQueryService> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _llmServiceFactory = llmServiceFactory ?? throw new ArgumentNullException(nameof(llmServiceFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Create LLM service using factory
            _llmService = _llmServiceFactory.CreateLlmService();
        }

        /// <summary>
        /// Step 1: Analyze a natural language question and generate an explanation
        /// </summary>
        /// <param name="request">The natural language query request</param>
        /// <returns>An explanation of how the system understands the question</returns>
        public async Task<AnalysisResponse> AnalyzeQuestionAsync(NlQueryRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrWhiteSpace(request.Question))
                throw new ArgumentException("Question cannot be empty", nameof(request));

            if (request.DatabaseId <= 0)
                throw new ArgumentException("DatabaseId must be specified", nameof(request));

            try
            {
                _logger.LogInformation("Analyzing question: {Question} for database ID: {DatabaseId}",
                    request.Question, request.DatabaseId);

                // Get database metadata
                var metadata = await _repository.GetDatabaseMetadataAsync(request.DatabaseId);

                // Format schema for LLM
                var schemaText = FormatSchemaForLlm(metadata);

                // Extract admin descriptions
                var adminDescriptions = ExtractAdminDescriptions(metadata);

                // Generate explanation using LLM
                var explanation = await _llmService.GenerateExplanationAsync(
                    request.Question, schemaText, adminDescriptions);

                // Return analysis response
                return new AnalysisResponse
                {
                    Question = request.Question,
                    DatabaseId = request.DatabaseId,
                    Explanation = explanation.Explanation,
                    HasAmbiguities = explanation.HasAmbiguities,
                    DetectedAmbiguities = explanation.DetectedAmbiguities,
                    AdjustableParameters = explanation.AdjustableParameters,
                    PreviewSql = explanation.PreviewSql,
                    ConfidenceScore = explanation.ConfidenceScore,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing question: {Question}", request.Question);

                return new AnalysisResponse
                {
                    Question = request.Question,
                    DatabaseId = request.DatabaseId,
                    Explanation = "I couldn't analyze this question due to an error.",
                    ErrorMessage = ex.Message,
                    Success = false
                };
            }
        }

        /// <summary>
        /// Step 2: Generate SQL based on confirmed understanding
        /// </summary>
        /// <param name="request">The confirmation request with resolved ambiguities</param>
        /// <returns>Generated SQL query</returns>
        public async Task<SqlGenerationResponse> GenerateSqlAsync(NlQueryConfirmationRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrWhiteSpace(request.OriginalQuestion))
                throw new ArgumentException("Original question cannot be empty", nameof(request));

            if (string.IsNullOrWhiteSpace(request.ConfirmedUnderstanding))
                throw new ArgumentException("Confirmed understanding cannot be empty", nameof(request));

            if (request.DatabaseId <= 0)
                throw new ArgumentException("DatabaseId must be specified", nameof(request));

            try
            {
                _logger.LogInformation("Generating SQL for question: {Question} with confirmed understanding",
                    request.OriginalQuestion);

                // Get database metadata
                var metadata = await _repository.GetDatabaseMetadataAsync(request.DatabaseId);

                // Format schema for LLM
                var schemaText = FormatSchemaForLlm(metadata);

                // Generate SQL using LLM
                var sql = await _llmService.GenerateSqlAsync(
                    request.OriginalQuestion,
                    request.ConfirmedUnderstanding,
                    schemaText,
                    request.ResolvedAmbiguities);

                // Return SQL generation response
                return new SqlGenerationResponse
                {
                    OriginalQuestion = request.OriginalQuestion,
                    DatabaseId = request.DatabaseId,
                    GeneratedSql = sql,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating SQL for question: {Question}", request.OriginalQuestion);

                return new SqlGenerationResponse
                {
                    OriginalQuestion = request.OriginalQuestion,
                    DatabaseId = request.DatabaseId,
                    ErrorMessage = ex.Message,
                    Success = false
                };
            }
        }

        /// <summary>
        /// Step 3: Execute the generated SQL and explain the results
        /// </summary>
        /// <param name="request">The execution request with the SQL query</param>
        /// <returns>Query results with explanation</returns>
        public async Task<QueryExecutionResponse> ExecuteQueryAsync(SqlExecutionRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrWhiteSpace(request.Sql))
                throw new ArgumentException("SQL query cannot be empty", nameof(request));

            if (request.DatabaseId <= 0)
                throw new ArgumentException("DatabaseId must be specified", nameof(request));

            try
            {
                _logger.LogInformation("Executing SQL query for database ID: {DatabaseId}", request.DatabaseId);

                // Execute the query
                var results = await _repository.ExecuteQueryOnDatabaseAsync(request.Sql, request.DatabaseId);

                // Generate explanation for the results
                string explanation = null;
                if (!string.IsNullOrEmpty(request.OriginalQuestion))
                {
                    explanation = await _llmService.GenerateResultExplanationAsync(
                        request.OriginalQuestion, request.Sql, results);
                }

                // Determine appropriate data viewing type
                var (viewingTypeId, viewingTypeName, formattedResult) = DetermineDataViewingType(results, request.Sql);

                // Return execution response
                return new QueryExecutionResponse
                {
                    OriginalQuestion = request.OriginalQuestion,
                    DatabaseId = request.DatabaseId,
                    Sql = request.Sql,
                    Results = results,
                    ResultExplanation = explanation,
                    RecommendedDataViewingTypeID = viewingTypeId,
                    RecommendedDataViewingTypeName = viewingTypeName,
                    FormattedResult = formattedResult,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing SQL query for database ID: {DatabaseId}", request.DatabaseId);

                return new QueryExecutionResponse
                {
                    OriginalQuestion = request.OriginalQuestion,
                    DatabaseId = request.DatabaseId,
                    Sql = request.Sql,
                    ErrorMessage = ex.Message,
                    Success = false
                };
            }
        }

        /// <summary>
        /// Combined method for backward compatibility: Analyze, generate SQL, and execute in one step
        /// </summary>
        /// <param name="request">The natural language query request</param>
        /// <returns>Complete query response</returns>
        public async Task<NlQueryResponse> ProcessNaturalLanguageQueryAsync(NlQueryRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            try
            {
                // Get database metadata
                var metadata = await _repository.GetDatabaseMetadataAsync(request.DatabaseId);

                // Format schema for LLM
                var schemaText = FormatSchemaForLlm(metadata);

                // Extract admin descriptions
                var adminDescriptions = ExtractAdminDescriptions(metadata);

                // Generate explanation using LLM
                var explanation = await _llmService.GenerateExplanationAsync(
                    request.Question, schemaText, adminDescriptions);

                // Generate SQL (assuming user would confirm the explanation)
                var sql = await _llmService.GenerateSqlAsync(
                    request.Question,
                    explanation.Explanation,
                    schemaText,
                    null);

                // Execute the query
                var results = await _repository.ExecuteQueryOnDatabaseAsync(sql, request.DatabaseId);

                // Generate explanation for the results
                var resultExplanation = await _llmService.GenerateResultExplanationAsync(
                    request.Question, sql, results);

                // Determine appropriate data viewing type
                var (viewingTypeId, viewingTypeName, formattedResult) = DetermineDataViewingType(results, sql);

                // Return complete response with backward compatibility
                return new NlQueryResponse
                {
                    FormattedQuestion = request.Question,
                    GeneratedSql = sql,
                    Results = results,
                    Explanation = resultExplanation,
                    Success = true,
                    TemplateInfo = CreateTemplateInfoFromExplanation(explanation),
                    RecommendedDataViewingTypeID = viewingTypeId,
                    RecommendedDataViewingTypeName = viewingTypeName,
                    FormattedResult = formattedResult
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing natural language query: {Question}", request.Question);

                return new NlQueryResponse
                {
                    FormattedQuestion = request.Question,
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        #region Helper Methods

        private string FormatSchemaForLlm(Dictionary<string, object> metadata)
        {
            var schema = new StringBuilder();

            // Extract tables from metadata
            if (metadata.TryGetValue("tables", out var tablesObj) && tablesObj is List<Table> tables)
            {
                schema.AppendLine("Tables:");

                foreach (var table in tables)
                {
                    var tableName = !string.IsNullOrEmpty(table.AdminTableName) ?
                        $"{table.DBTableName} (Admin: \"{table.AdminTableName}\")" :
                        table.DBTableName;

                    schema.Append($"- {tableName}");

                    if (!string.IsNullOrEmpty(table.AdminDescription))
                    {
                        schema.Append($" - {table.AdminDescription}");
                    }

                    schema.AppendLine();

                    // Append columns if available
                    if (metadata.TryGetValue("allColumns", out var columnsObj) &&
                        columnsObj is List<Column> allColumns)
                    {
                        var tableColumns = allColumns.FindAll(c => c.TableID == table.TableID);

                        foreach (var column in tableColumns)
                        {
                            var columnName = !string.IsNullOrEmpty(column.AdminColumnName) ?
                                $"{column.DBColumnName} (Admin: \"{column.AdminColumnName}\")" :
                                column.DBColumnName;

                            schema.Append($"  - {columnName}: {column.DataType}");

                            if (!string.IsNullOrEmpty(column.AdminDescription))
                            {
                                schema.Append($" - {column.AdminDescription}");
                            }

                            schema.AppendLine();
                        }
                    }

                    schema.AppendLine();
                }
            }

            // Add relationships
            if (metadata.TryGetValue("allRelationships", out var relationshipsObj) &&
                relationshipsObj is List<Relationship> relationships &&
                relationships.Count > 0)
            {
                schema.AppendLine("Relationships:");

                foreach (var relationship in relationships)
                {
                    // Simplified relationship format for LLM
                    schema.AppendLine($"- {relationship.TableID}.{relationship.ColumnID} -> " +
                        $"{relationship.RelatedTableID}.{relationship.RelatedColumnID}" +
                        $" ({relationship.RelationshipType})");
                }
            }

            return schema.ToString();
        }

        private Dictionary<string, string> ExtractAdminDescriptions(Dictionary<string, object> metadata)
        {
            var descriptions = new Dictionary<string, string>();

            // Extract table descriptions
            if (metadata.TryGetValue("tables", out var tablesObj) && tablesObj is List<Table> tables)
            {
                foreach (var table in tables)
                {
                    if (!string.IsNullOrEmpty(table.AdminTableName))
                    {
                        descriptions[table.DBTableName] = table.AdminTableName;
                    }

                    if (!string.IsNullOrEmpty(table.AdminDescription))
                    {
                        descriptions[$"{table.DBTableName} description"] = table.AdminDescription;
                    }
                }
            }

            // Extract column descriptions
            if (metadata.TryGetValue("allColumns", out var columnsObj) && columnsObj is List<Column> columns)
            {
                foreach (var column in columns)
                {
                    if (!string.IsNullOrEmpty(column.AdminColumnName))
                    {
                        descriptions[column.DBColumnName] = column.AdminColumnName;
                    }

                    if (!string.IsNullOrEmpty(column.AdminDescription))
                    {
                        descriptions[$"{column.DBColumnName} description"] = column.AdminDescription;
                    }
                }
            }

            return descriptions;
        }

        private TemplateMatchInfo CreateTemplateInfoFromExplanation(ExplanationResponse explanation)
        {
            // For backward compatibility, create a TemplateMatchInfo from the explanation
            var templateInfo = new TemplateMatchInfo
            {
                Intent = "dynamic_query", // Default intent for compatibility
                ConfidenceScore = explanation.ConfidenceScore
            };

            // Add operations based on SQL preview
            if (!string.IsNullOrEmpty(explanation.PreviewSql))
            {
                var sql = explanation.PreviewSql.ToLowerInvariant();

                if (sql.Contains("select") && !sql.Contains("count("))
                {
                    templateInfo.Intent = "retrieve";
                }
                else if (sql.Contains("count("))
                {
                    templateInfo.Intent = "count";
                }
                else if (sql.Contains("sum(") || sql.Contains("avg(") ||
                         sql.Contains("min(") || sql.Contains("max("))
                {
                    templateInfo.Intent = "aggregate";
                }

                if (sql.Contains("where"))
                {
                    templateInfo.Operations.Add("filter");
                }

                if (sql.Contains("group by"))
                {
                    templateInfo.Operations.Add("group");
                }

                if (sql.Contains("order by"))
                {
                    templateInfo.Operations.Add("sort");
                }

                if (sql.Contains("top") || sql.Contains("limit"))
                {
                    templateInfo.Operations.Add("limit");
                }

                if (sql.Contains("join"))
                {
                    templateInfo.Operations.Add("join");
                }
            }

            // Add parameters from adjustable parameters
            if (explanation.AdjustableParameters != null)
            {
                foreach (var param in explanation.AdjustableParameters)
                {
                    templateInfo.Parameters.Add(new QueryParameter
                    {
                        Name = param.Key,
                        Value = param.Value.DefaultValue,
                        EntityType = param.Value.ParameterType
                    });
                }
            }

            return templateInfo;
        }

        private (int? ViewingTypeId, string ViewingTypeName, string FormattedResult) DetermineDataViewingType(
            List<Dictionary<string, object>> results,
            string query)
        {
            // If no results, default to table
            if (results == null || results.Count == 0)
            {
                return (null, "Table", null);
            }

            // Single result with single column might be a label or number
            if (results.Count == 1 && results[0].Count == 1)
            {
                var singleValue = results[0].Values.First();

                // Check for numeric types
                if (singleValue is int intVal)
                {
                    return ((int)DataViewingTypeEnum.Number, "Number", FormatNumber(intVal));
                }
                else if (singleValue is decimal decVal)
                {
                    return ((int)DataViewingTypeEnum.Number, "Number", FormatNumber(decVal));
                }
                else if (singleValue is double doubleVal)
                {
                    return ((int)DataViewingTypeEnum.Number, "Number", FormatNumber(doubleVal));
                }
                else if (singleValue is float floatVal)
                {
                    return ((int)DataViewingTypeEnum.Number, "Number", FormatNumber(floatVal));
                }
                else if (singleValue is long longVal)
                {
                    return ((int)DataViewingTypeEnum.Number, "Number", FormatNumber(longVal));
                }
                else
                {
                    // For other single values, use label
                    return ((int)DataViewingTypeEnum.Label, "Label", singleValue?.ToString());
                }
            }

            // Aggregate queries might need special handling
            if (IsAggregateQuery(query))
            {
                // Check if aggregate result is numeric
                var sampleValue = results[0].Values.First();
                if (sampleValue is int || sampleValue is decimal ||
                    sampleValue is double || sampleValue is float ||
                    sampleValue is long)
                {
                    return ((int)DataViewingTypeEnum.Number, "Number", FormatNumber(Convert.ToDecimal(sampleValue)));
                }
            }

            // Default to table for complex or multi-column results
            return ((int)DataViewingTypeEnum.Table, "Table", null);
        }

        private bool IsAggregateQuery(string query)
        {
            query = query.ToLowerInvariant();
            return query.Contains("count(") ||
                   query.Contains("sum(") ||
                   query.Contains("avg(") ||
                   query.Contains("max(") ||
                   query.Contains("min(");
        }

        private string FormatNumber(object numberValue)
        {
            try
            {
                // Convert to decimal using Convert.ToDecimal which handles multiple numeric types
                decimal number = Convert.ToDecimal(numberValue);

                // Use culture-specific formatting with two decimal places
                return number.ToString("N2", System.Globalization.CultureInfo.CurrentCulture);
            }
            catch (FormatException)
            {
                // If conversion fails, return the original value as string
                return numberValue?.ToString() ?? string.Empty;
            }
            catch (InvalidCastException)
            {
                // If conversion is not possible, return the original value as string
                return numberValue?.ToString() ?? string.Empty;
            }
        }

        #endregion
    }
}