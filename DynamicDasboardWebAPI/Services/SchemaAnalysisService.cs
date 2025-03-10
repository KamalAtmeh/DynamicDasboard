using DynamicDashboardCommon.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DynamicDasboardWebAPI.Services
{
    public class SchemaAnalysisService
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;
        private readonly ILogger<SchemaAnalysisService> _logger;
        private readonly DatabaseService _databaseService;
        private readonly TableService _tableService;
        private readonly ColumnService _columnService;
        private readonly RelationshipService _relationshipService;

        public SchemaAnalysisService(
            IConfiguration config,
            HttpClient httpClient,
            ILogger<SchemaAnalysisService> logger,
            DatabaseService databaseService,
            TableService tableService,
            ColumnService columnService,
            RelationshipService relationshipService)
        {
            _config = config;
            _httpClient = httpClient;
            _logger = logger;
            _databaseService = databaseService;
            _tableService = tableService;
            _columnService = columnService;
            _relationshipService = relationshipService;
        }

        private string _apiKey => _config["DeepSeek:ApiKey"];
        private string _endpoint => _config["DeepSeek:Endpoint"];

        public async Task<SchemaAnalysisResult> AnalyzeDatabaseSchemaAsync(int databaseId)
        {
            try
            {
                // Get all tables, columns, and relationships from the database
                var database = await _databaseService.GetDatabaseByIdAsync(databaseId);
                if (database == null)
                {
                    return new SchemaAnalysisResult
                    {
                        Success = false,
                        ErrorMessage = $"Database with ID {databaseId} not found"
                    };
                }

                var tables = (await _tableService.GetTablesByDatabaseIdAsync(databaseId)).ToList();

                // Collect all columns for all tables
                var allColumns = new List<Column>();
                foreach (var table in tables)
                {
                    var columns = await _columnService.GetColumnsByTableIdAsync(table.TableID);
                    allColumns.AddRange(columns);
                }

                // Get all relationships
                var allRelationships = new List<Relationship>();
                foreach (var table in tables)
                {
                    var relationships = await _relationshipService.GetRelationshipsByTableIdAsync(table.TableID);
                    allRelationships.AddRange(relationships);
                }

                // Format the schema for LLM analysis
                var schemaDescription = FormatSchemaForAnalysis(tables, allColumns, allRelationships);

                // Analyze using LLM
                var analysisResult = await AnalyzeSchemaWithLLMAsync(schemaDescription);

                return analysisResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing database schema");
                return new SchemaAnalysisResult
                {
                    Success = false,
                    ErrorMessage = $"Error analyzing schema: {ex.Message}"
                };
            }
        }

        private string FormatSchemaForAnalysis(List<Table> tables, List<Column> columns, List<Relationship> relationships)
        {
            var sb = new StringBuilder();
            sb.AppendLine("DATABASE SCHEMA:");
            sb.AppendLine();

            // Format tables and columns
            sb.AppendLine("TABLES:");
            foreach (var table in tables)
            {
                sb.AppendLine($"- Table: {table.DBTableName} (Display Name: {table.AdminTableName ?? "Not set"}, Description: {table.AdminDescription ?? "Not set"})");

                // Get columns for this table
                var tableColumns = columns.Where(c => c.TableID == table.TableID).ToList();
                foreach (var column in tableColumns)
                {
                    sb.AppendLine($"  - Column: {column.DBColumnName} (Type: {column.DataType}, Nullable: {column.IsNullable}, Display Name: {column.AdminColumnName ?? "Not set"}, Description: {column.AdminDescription ?? "Not set"})");
                }
                sb.AppendLine();
            }

            // Format relationships
            sb.AppendLine("RELATIONSHIPS:");
            foreach (var relationship in relationships)
            {
                var sourceTable = tables.FirstOrDefault(t => t.TableID == relationship.TableID);
                var targetTable = tables.FirstOrDefault(t => t.TableID == relationship.RelatedTableID);
                var sourceColumn = columns.FirstOrDefault(c => c.ColumnID == relationship.ColumnID);
                var targetColumn = columns.FirstOrDefault(c => c.ColumnID == relationship.RelatedColumnID);

                if (sourceTable != null && targetTable != null && sourceColumn != null && targetColumn != null)
                {
                    sb.AppendLine($"- {sourceTable.DBTableName}.{sourceColumn.DBColumnName} -> {targetTable.DBTableName}.{targetColumn.DBColumnName} (Type: {relationship.RelationshipType})");
                }
            }

            return sb.ToString();
        }

        private async Task<SchemaAnalysisResult> AnalyzeSchemaWithLLMAsync(string schemaDescription)
        {
            try
            {
                var systemMessage = @"
You are a database expert specialized in analyzing database schemas and providing human-friendly descriptions. 
Your task is to analyze the provided database schema and:

1. Suggest clear, business-friendly descriptions for each table and column.
2. Identify potential conflicts or ambiguities in naming (e.g., similar table names, columns with similar purposes).
3. Validate relationships and suggest missing relationships based on column names and data types.
4. Identify columns or tables that might be difficult to understand and suggest clarifications.

For each table and column, consider its technical name and infer its business purpose. Use common naming conventions
and domain knowledge to provide meaningful descriptions. Focus on making the database more understandable to non-technical users.

Format your response as a JSON object with the following structure:
{
  ""tableDescriptions"": [
    {
      ""tableName"": ""string"",
      ""suggestedName"": ""string"",
      ""suggestedDescription"": ""string""
    }
  ],
  ""columnDescriptions"": [
    {
      ""tableName"": ""string"",
      ""columnName"": ""string"",
      ""suggestedName"": ""string"",
      ""suggestedDescription"": ""string"",
      ""isLookupColumn"": boolean
    }
  ],
  ""potentialConflicts"": [
    {
      ""type"": ""string"", // ""Table"" or ""Column""
      ""items"": [
        {
          ""name"": ""string"",
          ""tableName"": ""string"", // For columns
          ""suggestedResolution"": ""string""
        }
      ],
      ""conflictDescription"": ""string""
    }
  ],
  ""suggestedRelationships"": [
    {
      ""sourceTable"": ""string"",
      ""sourceColumn"": ""string"",
      ""targetTable"": ""string"",
      ""targetColumn"": ""string"",
      ""relationshipType"": ""string"",
      ""confidence"": number,
      ""reasoning"": ""string""
    }
  ],
  ""unclearElements"": [
    {
      ""type"": ""string"", // ""Table"" or ""Column""
      ""name"": ""string"",
      ""tableName"": ""string"", // For columns
      ""issue"": ""string"",
      ""suggestion"": ""string""
    }
  ]
}";

                var userMessage = $"Here is the database schema to analyze:\n\n{schemaDescription}";

                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);

                var requestBody = new
                {
                    model = "deepseek-chat",
                    messages = new[]
                    {
                        new { role = "system", content = systemMessage },
                        new { role = "user", content = userMessage }
                    },
                    temperature = 0.1,
                    max_tokens = 4000
                };

                var response = await _httpClient.PostAsJsonAsync(_endpoint, requestBody);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<DeepSeekResponse>(content);

                    var jsonResponse = result?.choices[0].message.content;

                    // Extract JSON from response (in case there's surrounding text)
                    jsonResponse = ExtractJsonFromResponse(jsonResponse);

                    // Parse the JSON response
                    var analysisResult = new SchemaAnalysisResult
                    {
                        Success = true,
                        RawLLMResponse = jsonResponse,
                        AnalysisData = JsonSerializer.Deserialize<SchemaAnalysisData>(jsonResponse,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    };

                    return analysisResult;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Error from LLM API: {StatusCode} - {Content}",
                        response.StatusCode, errorContent);

                    return new SchemaAnalysisResult
                    {
                        Success = false,
                        ErrorMessage = $"Error from LLM API: {response.StatusCode} - {errorContent}"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing schema with LLM");
                return new SchemaAnalysisResult
                {
                    Success = false,
                    ErrorMessage = $"Error analyzing schema with LLM: {ex.Message}"
                };
            }
        }

        private string ExtractJsonFromResponse(string response)
        {
            if (string.IsNullOrEmpty(response))
                return "{}";

            // Find first { and last }
            int start = response.IndexOf('{');
            int end = response.LastIndexOf('}');

            if (start >= 0 && end > start)
            {
                return response.Substring(start, end - start + 1);
            }

            return response; // Return as is if no JSON found
        }

        public async Task<bool> ApplyDescriptionsAsync(int databaseId, SchemaAnalysisData analysisData)
        {
            try
            {
                // Get all tables and columns
                var tables = (await _tableService.GetTablesByDatabaseIdAsync(databaseId)).ToList();

                // Apply table descriptions
                foreach (var tableDesc in analysisData.TableDescriptions)
                {
                    var table = tables.FirstOrDefault(t => t.DBTableName.Equals(tableDesc.TableName, StringComparison.OrdinalIgnoreCase));
                    if (table != null)
                    {
                        table.AdminTableName = tableDesc.SuggestedName;
                        table.AdminDescription = tableDesc.SuggestedDescription;
                        await _tableService.UpdateTableAsync(table);
                    }
                }

                // Apply column descriptions
                foreach (var columnDesc in analysisData.ColumnDescriptions)
                {
                    var table = tables.FirstOrDefault(t => t.DBTableName.Equals(columnDesc.TableName, StringComparison.OrdinalIgnoreCase));
                    if (table != null)
                    {
                        var columns = (await _columnService.GetColumnsByTableIdAsync(table.TableID)).ToList();
                        var column = columns.FirstOrDefault(c => c.DBColumnName.Equals(columnDesc.ColumnName, StringComparison.OrdinalIgnoreCase));

                        if (column != null)
                        {
                            column.AdminColumnName = columnDesc.SuggestedName;
                            column.AdminDescription = columnDesc.SuggestedDescription;
                            // Note: This would require adding IsLookupColumn to the Column model
                            // column.IsLookupColumn = columnDesc.IsLookupColumn;
                            await _columnService.UpdateColumnAsync(column);
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying descriptions");
                return false;
            }
        }

        public async Task<bool> AddSuggestedRelationshipAsync(int databaseId, SuggestedRelationship relationshipData)
        {
            try
            {
                // Get tables
                var tables = (await _tableService.GetTablesByDatabaseIdAsync(databaseId)).ToList();

                // Find source table
                var sourceTable = tables.FirstOrDefault(t => t.DBTableName.Equals(relationshipData.SourceTable.Table, StringComparison.OrdinalIgnoreCase));
                if (sourceTable == null) return false;

                // Find target table
                var targetTable = tables.FirstOrDefault(t => t.DBTableName.Equals(relationshipData.TargetTable.Table, StringComparison.OrdinalIgnoreCase));
                if (targetTable == null) return false;

                // Find source column
                var sourceColumns = await _columnService.GetColumnsByTableIdAsync(sourceTable.TableID);
                var sourceColumn = sourceColumns.FirstOrDefault(c => c.DBColumnName.Equals(relationshipData.SourceTable.Column, StringComparison.OrdinalIgnoreCase));
                if (sourceColumn == null) return false;

                // Find target column
                var targetColumns = await _columnService.GetColumnsByTableIdAsync(targetTable.TableID);
                var targetColumn = targetColumns.FirstOrDefault(c => c.DBColumnName.Equals(relationshipData.TargetTable.Column, StringComparison.OrdinalIgnoreCase));
                if (targetColumn == null) return false;

                // Create relationship
                var relationship = new Relationship
                {
                    TableID = sourceTable.TableID,
                    ColumnID = sourceColumn.ColumnID,
                    RelatedTableID = targetTable.TableID,
                    RelatedColumnID = targetColumn.ColumnID,
                    RelationshipType = relationshipData.RelationshipType,
                    Description = relationshipData.Reasoning,
                    IsEnforced = false, // Since this is a suggested relationship
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = 1 // System user or admin user ID
                };

                await _relationshipService.AddRelationshipAsync(relationship);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding suggested relationship");
                return false;
            }
        }
    }

}