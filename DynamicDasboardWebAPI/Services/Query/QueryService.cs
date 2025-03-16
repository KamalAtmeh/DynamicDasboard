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
using Microsoft.AspNetCore.Connections;
using System.Text.Json;
using static DynamicDashboardCommon.Helper.ApplicationHelper;
using MySqlX.XDevAPI;


namespace DynamicDasboardWebAPI.Services
{
    /// <summary>
    /// Enhanced service for processing natural language queries with a two-step confirmation process
    /// </summary>
    public class QueryService
    {
        private readonly QueryRepository _repository;
        private readonly LLMServiceFactory _llmServiceFactory;
        private readonly ILLMService _llmService;
        private readonly DatabaseService objDataBaseService;
        private readonly DatabaseSchemaService objSchemaService;

        public QueryService(
            QueryRepository repository,
            DatabaseService databaseService,
            DatabaseSchemaService schemaService,
            LLMServiceFactory llmServiceFactory
            )
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            objDataBaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            objSchemaService = schemaService ?? throw new ArgumentNullException(nameof(schemaService));
            _llmServiceFactory = llmServiceFactory ?? throw new ArgumentNullException(nameof(llmServiceFactory));

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

            try
            {

                var adminDescriptions = new Dictionary<string, string>();
                var schemaText = string.Empty;
                // Get database metadata
                // DatabaseMetadataDto metadata = await _repository.GetDatabaseMetadataAsync(request.DatabaseId);

                DatabaseSchema objSchema = await objSchemaService.GetSchemaObject(request.DatabaseId);

                // Format schema for LLM
                if (objSchema != null)
                {
                    schemaText = objSchemaService.BuildOptimizedSchemaString(objSchema);
                }
                else
                {
                    Database objDataBase = await objDataBaseService.GetDatabaseByIdAsync(request.DatabaseId);
                    objSchema = await objSchemaService.GenerateAndGetDatabaseSchemaFromConnectedDBAsync(request.DatabaseId, objDataBase);
                    schemaText = objSchemaService.BuildOptimizedSchemaString(objSchema);
                }



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
                throw;
            }
        }

        /// <summary>
        /// Step 2: Generate SQL based on confirmed understanding
        /// </summary>
        /// <param name="request">The confirmation request with resolved ambiguities</param>
        /// <returns>Generated SQL query</returns>
        public async Task<SqlGenerationResponse> GenerateSqlAsync(NlQueryConfirmationRequest request)
        {
            try
            {

                var schemaText = string.Empty;
                // Get database metadata
                var schemaObj = await objSchemaService.GetSchemaObject(request.DatabaseId);
                if (schemaObj != null && !string.IsNullOrEmpty(schemaObj.SchemaData))
                {

                    // Optimize schema for LLM
                    schemaText = objSchemaService.BuildOptimizedSchemaString(schemaObj);


                }
                else
                {
                    // Fallback to metadata if no saved schema
                    Database objDataBase = await objDataBaseService.GetDatabaseByIdAsync(request.DatabaseId);
                    schemaObj = await objSchemaService.GenerateAndGetDatabaseSchemaFromConnectedDBAsync(request.DatabaseId, objDataBase);

                    schemaText = objSchemaService.BuildOptimizedSchemaString(schemaObj);
                }

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
                throw;
            }
        }

        /// <summary>
        /// Step 3: Execute the generated SQL and explain the results
        /// </summary>
        /// <param name="request">The execution request with the SQL query</param>
        /// <returns>Query results with explanation</returns>
        public async Task<QueryExecutionResponse> ExecuteQueryAsync(SqlExecutionRequest request)
        {
            try
            {
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
                throw;
            }
        }

        /// <summary>
        /// Combined method for backward compatibility: Analyze, generate SQL, and execute in one step
        /// </summary>
        /// <param name="request">The natural language query request</param>
        /// <returns>Complete query response</returns>
        public async Task<NlQueryResponse> ProcessNaturalLanguageQueryAsync(NlQueryRequest request)
        {


            try
            {
                var adminDescriptions = new Dictionary<string, string>();
                var schemaText = string.Empty;
                // Get database metadata
                var schemaObj = await objSchemaService.GetSchemaObject(request.DatabaseId);
                if (schemaObj != null && !string.IsNullOrEmpty(schemaObj.SchemaData))
                {

                    // Optimize schema for LLM
                    schemaText = objSchemaService.BuildOptimizedSchemaString(schemaObj);


                }
                else
                {
                    // Fallback to metadata if no saved schema
                    Database objDataBase = await objDataBaseService.GetDatabaseByIdAsync(request.DatabaseId);
                    schemaObj = await objSchemaService.GenerateAndGetDatabaseSchemaFromConnectedDBAsync(request.DatabaseId, objDataBase);

                    schemaText = objSchemaService.BuildOptimizedSchemaString(schemaObj);
                }

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
                throw;
            }
        }

        /// <summary>
        /// Generates SQL with explanation from a natural language question
        /// </summary>
        /// <param name="request">The natural language query request</param>
        /// <returns>SQL explanation response with validation status</returns>
        /// <summary>
        /// Generates SQL with explanation from a natural language question
        /// </summary>
        /// <param name="request">The natural language query request</param>
        /// <returns>SQL explanation response with validation status</returns>
        public async Task<SqlGenerationWithExplanationResponse> GenerateSqlWithExplanationAsync(NlQueryRequest request)
        {
            try
            {
                // Get database and schema information
                var database = await objDataBaseService.GetDatabaseByIdAsync(request.DatabaseId);
                if (database == null)
                {
                    return new SqlGenerationWithExplanationResponse
                    {
                        Success = false,
                        ErrorMessage = "Database not found"
                    };
                }

                // Check if there's a saved schema

                var schemaString = "";
                var adminDescriptions = new Dictionary<string, string>();

                // Try to get schema from database
                var schemaObj = await objSchemaService.GetSchemaObject(request.DatabaseId);
                if (schemaObj != null && !string.IsNullOrEmpty(schemaObj.SchemaData))
                {

                    // Optimize schema for LLM
                    schemaString = objSchemaService.BuildOptimizedSchemaString(schemaObj);

                    if (schemaObj.TermMappings?.Any() == true)
                    {
                        schemaString = EnhanceSchemaWithTermMappings(schemaString, schemaObj);
                    }


                }
                else
                {
                    // Fallback to metadata if no saved schema
                    Database objDataBase = await objDataBaseService.GetDatabaseByIdAsync(request.DatabaseId);
                    schemaObj = await objSchemaService.GenerateAndGetDatabaseSchemaFromConnectedDBAsync(request.DatabaseId, objDataBase);

                        schemaString = objSchemaService.BuildOptimizedSchemaString(schemaObj);
                }

                if (string.IsNullOrEmpty(schemaString))
                {
                    return new SqlGenerationWithExplanationResponse
                    {
                        Success = false,
                        ErrorMessage = "Could not retrieve database schema"
                    };
                }

                // Generate SQL with explanation, including schema relevance analysis
                var llmResponse = await _llmService.GenerateSqlWithExplanationAsync(
                    request.Question,
                    schemaString,
                    adminDescriptions);

                if (llmResponse == null)
                {
                    return new SqlGenerationWithExplanationResponse
                    {
                        Success = false,
                        ErrorMessage = "Failed to generate response from LLM"
                    };
                }

                // If the question is not related to the schema or SQL is empty, return as is
                //TODO To handle this case
                if (!llmResponse.IsSchemaRelated || string.IsNullOrEmpty(llmResponse.SqlQuery))
                {
                    return new SqlGenerationWithExplanationResponse
                    {
                        Success = true,
                        OriginalQuestion = request.Question,
                        DatabaseId = request.DatabaseId,
                        IsSchemaRelated = llmResponse.IsSchemaRelated,
                        SchemaRelevanceMessage = llmResponse.SchemaRelevanceMessage,
                        HasPartiallyUnrelatedContent = llmResponse.HasPartiallyUnrelatedContent,
                        UnrelatedQuestionParts = llmResponse.UnrelatedQuestionParts,
                        SuggestedTopics = llmResponse.SuggestedTopics,
                        SuggestedQuestions = llmResponse.SuggestedQuestions,
                        BusinessExplanation = llmResponse.BusinessExplanation,
                        DbType = llmResponse.DbType,
                        SqlQuery = llmResponse.SqlQuery
                    };
                }

                // For related questions with SQL, validate the SQL
                bool isValid = true;
                string validationErrorMessage = null;

                try
                {
                    if (!string.IsNullOrEmpty(llmResponse.SqlQuery))
                    {
                        // Validate SQL
                        var validationResult = await ValidateSqlAgainstSchemaAsync(
                            llmResponse.SqlQuery, request.DatabaseId);

                        if (!validationResult.IsValid)
                        {
                            isValid = false;
                            validationErrorMessage = validationResult.ErrorMessage;
                        }
                    }
                }
                catch (Exception ex)
                {
                    isValid = false;
                    validationErrorMessage = ex.Message;
                }

                // Convert parameter options
                var adjustableParameters = new Dictionary<string, QueryParameterOptions>();
                foreach (var param in llmResponse.AdjustableParameters)
                {
                    adjustableParameters[param.Key] = new QueryParameterOptions
                    {
                        DefaultValue = param.Value.DefaultValue?.ToString(),
                        Alternatives = param.Value.Alternatives,
                        ParameterType = param.Value.ParameterType
                    };
                }

                // Build the full response
                return new SqlGenerationWithExplanationResponse
                {
                    OriginalQuestion = request.Question,
                    DatabaseId = request.DatabaseId,
                    SqlQuery = llmResponse.SqlQuery,
                    BusinessExplanation = llmResponse.BusinessExplanation,
                    DbType = llmResponse.DbType,
                    DbNotes = llmResponse.DbNotes,
                    HasAmbiguities = llmResponse.HasAmbiguities,
                    DetectedAmbiguities = llmResponse.DetectedAmbiguities,
                    AdjustableParameters = adjustableParameters,
                    TermMapping = llmResponse.TermMapping,
                    IsValid = isValid,
                    ValidationErrorMessage = validationErrorMessage,
                    IsSchemaRelated = llmResponse.IsSchemaRelated,
                    SchemaRelevanceMessage = llmResponse.SchemaRelevanceMessage,
                    HasPartiallyUnrelatedContent = llmResponse.HasPartiallyUnrelatedContent,
                    UnrelatedQuestionParts = llmResponse.UnrelatedQuestionParts,
                    SuggestedTopics = llmResponse.SuggestedTopics,
                    SuggestedQuestions = llmResponse.SuggestedQuestions,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        /// <summary>
        /// Validates a SQL query against a database without retrieving results
        /// </summary>
        /// <param name="sql">The SQL query to validate</param>
        /// <param name="databaseId">The database ID</param>
        /// <returns>Task representing the validation</returns>
        /// TODO change the implementation of this method to be more dynamic if applicable
        private async Task ValidateSqlAsync(string sql, int databaseId)
        {
            //if (string.IsNullOrWhiteSpace(sql))
            //    throw new ArgumentException("SQL query cannot be empty");

            //// Get database connection
            //using var connection = await _connectionFactory.CreateOpenConnectionAsync(databaseId);
            //if (connection == null)
            //    throw new Exception("Could not connect to database");

            //// Create a command to validate the SQL
            //using var command = connection.CreateCommand();
            //command.CommandText = $"SET FMTONLY ON; {sql}; SET FMTONLY OFF;";
            //command.CommandTimeout = 30;

            //// Execute the command (will throw if SQL is invalid)
            //await Task.Run(() => command.ExecuteNonQuery());
            await Task.CompletedTask;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlValidationService"/> class
        /// </summary>
        /// <param name="schemaService">The database schema service</param>

        /// <summary>
        /// Validates SQL query syntax without checking against the schema
        /// </summary>
        /// <param name="sqlQuery">The SQL query to validate</param>
        /// <returns>Validation result with error details if any</returns>
        public QueryValidationResult ValidateSqlSyntax(string sqlQuery)
        {
            try
            {
                return SqlValidationHelper.ValidateSqlSyntax(sqlQuery);
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
        /// Validates SQL query against the schema of a specified database
        /// </summary>
        /// <param name="sqlQuery">The SQL query to validate</param>
        /// <param name="databaseId">The database ID</param>
        /// <param name="validateRelations">Whether to validate relationship constraints</param>
        /// <returns>Validation result with error details if any</returns>
        public async Task<QueryValidationResult> ValidateSqlAgainstSchemaAsync(string sqlQuery, int databaseId, bool validateRelations = false)
        {
            try
            {
                // Validate basic syntax first
                var syntaxResult = SqlValidationHelper.ValidateSqlSyntax(sqlQuery);
                if (!syntaxResult.IsValid)
                {
                    return syntaxResult;
                }

                // Get database schema
                var schema = await objSchemaService.GetSchemaObject(databaseId);
                if (schema == null || string.IsNullOrEmpty(schema.SchemaData))
                {
                    return new QueryValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "Database schema not found"
                    };
                }
                if (schema.Tables == null || schema.Tables.Count == 0)
                {
                    return new QueryValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "Invalid or empty database schema"
                    };
                }

                // Validate SQL against schema
                return SqlValidationHelper.ValidateSqlAgainstSchema(sqlQuery, schema, validateRelations);
            }
            catch (Exception ex)
            {
                return new QueryValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"SQL validation error: {ex.Message}"
                };
            }
        }

        #region Term Mapping

        // Add to DynamicDasboardWebAPI/Services/Query/QueryService.cs
        private string EnhanceSchemaWithTermMappings(string schemaText, DatabaseSchema schema)
        {
            if (schema?.TermMappings == null || !schema.TermMappings.Any())
                return string.Empty;

            var additionalInfo = new StringBuilder();

            additionalInfo.AppendLine("\nBusiness Term Mappings:");

            // Add direct column mappings
            var directMappings = schema.TermMappings
                .Where(t => t.IsActive && t.Type == TermMappingType.DirectColumn)
                .ToList();

            if (directMappings.Any())
            {
                additionalInfo.AppendLine("  Direct Column Mappings:");
                foreach (var mapping in directMappings)
                {
                    var table = schema.Tables.FirstOrDefault(t => t.ID == mapping.TableId);
                    var column = table?.Columns?.FirstOrDefault(c => c.ID == mapping.ColumnId);

                    if (table != null && column != null)
                    {
                        additionalInfo.AppendLine($"    - '{mapping.BusinessTerm}' refers to '{table.DBName}.{column.DBName}'");

                        if (mapping.Synonyms?.Any() == true)
                        {
                            additionalInfo.Append("      Synonyms: ");
                            additionalInfo.AppendLine(string.Join(", ", mapping.Synonyms.Select(s => $"'{s}'")));
                        }
                    }
                }
            }

            // Add calculated field mappings
            var calculatedMappings = schema.TermMappings
                .Where(t => t.IsActive && t.Type == TermMappingType.CalculatedField)
                .ToList();

            if (calculatedMappings.Any())
            {
                additionalInfo.AppendLine("  Calculated Field Mappings:");
                foreach (var mapping in calculatedMappings)
                {
                    additionalInfo.AppendLine($"    - '{mapping.BusinessTerm}' is calculated as: {mapping.Formula}");

                    if (mapping.Dependencies?.Any() == true)
                    {
                        additionalInfo.Append("      Depends on: ");
                        additionalInfo.AppendLine(string.Join(", ", mapping.Dependencies.Select(d => $"'{d.TableName}.{d.ColumnName}'")));
                    }

                    if (mapping.Synonyms?.Any() == true)
                    {
                        additionalInfo.Append("      Synonyms: ");
                        additionalInfo.AppendLine(string.Join(", ", mapping.Synonyms.Select(s => $"'{s}'")));
                    }
                }
            }

            // Add filter condition mappings
            var filterMappings = schema.TermMappings
                .Where(t => t.IsActive && t.Type == TermMappingType.FilterCondition)
                .ToList();

            if (filterMappings.Any())
            {
                additionalInfo.AppendLine("  Filter Condition Mappings:");
                foreach (var mapping in filterMappings)
                {
                    additionalInfo.AppendLine($"    - '{mapping.BusinessTerm}' means: {mapping.FilterCondition}");

                    if (mapping.Synonyms?.Any() == true)
                    {
                        additionalInfo.Append("      Synonyms: ");
                        additionalInfo.AppendLine(string.Join(", ", mapping.Synonyms.Select(s => $"'{s}'")));
                    }
                }
            }

            // Add aggregate mappings
            var aggregateMappings = schema.TermMappings
                .Where(t => t.IsActive && t.Type == TermMappingType.Aggregate)
                .ToList();

            if (aggregateMappings.Any())
            {
                additionalInfo.AppendLine("  Aggregate Mappings:");
                foreach (var mapping in aggregateMappings)
                {
                    additionalInfo.AppendLine($"    - '{mapping.BusinessTerm}' refers to an aggregation");

                    if (mapping.Dependencies?.Any() == true)
                    {
                        additionalInfo.Append("      Applies to: ");
                        additionalInfo.AppendLine(string.Join(", ", mapping.Dependencies.Select(d => $"'{d.TableName}.{d.ColumnName}'")));
                    }

                    if (mapping.Synonyms?.Any() == true)
                    {
                        additionalInfo.Append("      Synonyms: ");
                        additionalInfo.AppendLine(string.Join(", ", mapping.Synonyms.Select(s => $"'{s}'")));
                    }
                }
            }

            return schemaText + "\n" + additionalInfo.ToString();
        }

        #endregion



        #region Helper Methods

        private string FormatSchemaForLlm(DatabaseMetadataDto metadata)
        {
            try
            {


                var schemaBuilder = new StringBuilder();
                schemaBuilder.AppendLine("Database Schema:");

                foreach (var tableMetadata in metadata.Tables)
                {
                    var table = tableMetadata.Table;
                    // Table header
                    schemaBuilder.Append($"- {table.DBTableName}");
                    if (!string.IsNullOrWhiteSpace(table.AdminTableName))
                    {
                        schemaBuilder.Append($" (Admin Name: {table.AdminTableName})");
                    }
                    if (!string.IsNullOrWhiteSpace(table.AdminDescription))
                    {
                        schemaBuilder.Append($" - {table.AdminDescription}");
                    }
                    schemaBuilder.AppendLine();

                    // Columns
                    if (tableMetadata.Columns != null)
                    {
                        schemaBuilder.AppendLine("  Columns:");
                        foreach (var column in tableMetadata.Columns)
                        {
                            schemaBuilder.Append($"    - {column.DBColumnName} ({column.DataType})");

                            if (!string.IsNullOrWhiteSpace(column.AdminColumnName))
                            {
                                schemaBuilder.Append($" (Admin Name: {column.AdminColumnName})");
                            }

                            if (!string.IsNullOrWhiteSpace(column.AdminDescription))
                            {
                                schemaBuilder.Append($" - {column.AdminDescription}");
                            }

                            schemaBuilder.AppendLine();
                        }
                    }

                    // Relationships
                    if (tableMetadata.Relationships != null && tableMetadata.Relationships.Any())
                    {
                        schemaBuilder.AppendLine("  Relationships:");
                        foreach (var relationship in tableMetadata.Relationships)
                        {
                            schemaBuilder.AppendLine(
                                $"    - {relationship.RelationshipType}: " +
                                $"Table {relationship.TableID}, Column {relationship.ColumnID} " +
                                $"-> Related Table {relationship.RelatedTableID}, Column {relationship.RelatedColumnID}"
                            );
                        }
                    }

                    schemaBuilder.AppendLine(); // Separator between tables
                }
                return schemaBuilder.ToString();
            }
            catch (Exception ex)
            {
                throw;
            }
        }


        private Dictionary<string, string> ExtractAdminDescriptions(List<TableMetadataDto> tableMetadataDtos)
        {
            var descriptions = new Dictionary<string, string>();

            try
            {
                if (tableMetadataDtos == null || !tableMetadataDtos.Any())
                {
                    return descriptions;
                }

                // Extract table descriptions
                foreach (var tableMetadata in tableMetadataDtos)
                {
                    var table = tableMetadata.Table;
                    if (table != null && !string.IsNullOrEmpty(table.DBTableName))
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

                    // Extract column descriptions for this table
                    if (tableMetadata.Columns != null)
                    {
                        foreach (var column in tableMetadata.Columns)
                        {
                            if (column != null && !string.IsNullOrEmpty(column.DBColumnName))
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
                    }
                }

                return descriptions;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private TemplateMatchInfo CreateTemplateInfoFromExplanation(ExplanationResponse explanation)
        {
            try
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
            catch (Exception ex)
            {
                throw;
            }
        }

        private (int? ViewingTypeId, string ViewingTypeName, string FormattedResult) DetermineDataViewingType(
            List<Dictionary<string, object>> results,
            string query)
        {
            try
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
            catch (Exception ex)
            {
                throw;
            }
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

        //to move into helper if this is needed
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