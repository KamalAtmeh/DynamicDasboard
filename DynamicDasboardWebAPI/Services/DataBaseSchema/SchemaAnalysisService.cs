using DynamicDashboardCommon.Models;
using DynamicDasboardWebAPI.Services.LLM;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MySqlX.XDevAPI;

namespace DynamicDasboardWebAPI.Services
{
    /// <summary>
    /// Service for analyzing database schemas using LLM
    /// </summary>
    public class SchemaAnalysisService
    {
        private readonly DatabaseSchemaService _schemaService;
        private readonly DatabaseService _databaseService;
        private readonly LLMServiceFactory _llmServiceFactory;
        private readonly ILLMService _llmService;
        private readonly ILogger<SchemaAnalysisService> _logger;

        public SchemaAnalysisService(
            DatabaseSchemaService schemaService,
            DatabaseService databaseService,
            LLMServiceFactory llmServiceFactory,
            ILogger<SchemaAnalysisService> logger)
        {
            _schemaService = schemaService ?? throw new ArgumentNullException(nameof(schemaService));
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _llmServiceFactory = llmServiceFactory ?? throw new ArgumentNullException(nameof(llmServiceFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Create LLM service using factory
            _llmService = _llmServiceFactory.CreateLlmService();
        }

        /// <summary>
        /// Analyzes a database schema using LLM to generate descriptions, friendly names, and identify conflicts
        /// </summary>
        /// <param name="databaseId">The ID of the database to analyze</param>
        /// <returns>Schema analysis result with suggestions</returns>
        public async Task<SchemaAnalysisResult> AnalyzeDatabaseSchemaAsync(int databaseId)
        {
            try
            {
                _logger.LogInformation($"Starting schema analysis for database ID: {databaseId}");

                // Get database schema
                var database = await _databaseService.GetDatabaseByIdAsync(databaseId);
                if (database == null)
                {
                    _logger.LogWarning($"Database with ID {databaseId} not found");
                    return new SchemaAnalysisResult
                    {
                        Success = false,
                        ErrorMessage = $"Database with ID {databaseId} not found"
                    };
                }

                // Get existing schema if available
                var schemaObj = await _schemaService.GetJsonSchemaByDataBaseIdAsync(databaseId);
                if (schemaObj == null || string.IsNullOrEmpty(schemaObj.SchemaData))
                {
                    _logger.LogInformation($"No existing schema found for database ID: {databaseId}. Generating new schema.");
                    schemaObj = await _schemaService.GenerateAndGetDatabaseSchemaFromConnectedDBAsync(databaseId, database);

                    if (schemaObj == null)
                    {
                        return new SchemaAnalysisResult
                        {
                            Success = false,
                            ErrorMessage = "Failed to generate schema from database"
                        };
                    }
                }
                 schemaObj =  _schemaService.DeserializeSchema(schemaObj.SchemaData);
                // Format schema for LLM analysis
                var schemaForAnalysis = _schemaService.BuildOptimizedSchemaString(schemaObj);

                // Call LLM to analyze schema
                var analysisResult = await AnalyzeSchemaWithLLMAsync(schemaForAnalysis, database.Name);

                return analysisResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error analyzing schema for database ID: {databaseId}");
                return new SchemaAnalysisResult
                {
                    Success = false,
                    ErrorMessage = $"Error analyzing schema: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Calls LLM service to analyze database schema
        /// </summary>
        private async Task<SchemaAnalysisResult> AnalyzeSchemaWithLLMAsync(string schema, string databaseName)
        {
            try
            {
                // Build prompt for LLM
                var prompt = BuildSchemaAnalysisPrompt(schema, databaseName);

                // Call LLM
                var response = await _llmService.GenerateSchemaAnalysisAsync(prompt);

                // Parse LLM response
                return ParseLLMSchemaAnalysisResponse(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in LLM schema analysis");
                return new SchemaAnalysisResult
                {
                    Success = false,
                    ErrorMessage = $"Error in LLM analysis: {ex.Message}",
                    RawLLMResponse = null
                };
            }
        }

        /// <summary>
        /// Builds a prompt for LLM to analyze database schema
        /// </summary>
        private string BuildSchemaAnalysisPrompt(string schema, string databaseName)
        {
            var prompt = new StringBuilder();

            prompt.AppendLine($"You are an expert database analyst helping improve the usability of a database named '{databaseName}'.");
            prompt.AppendLine("\nYour task is to analyze the database schema below and provide:");
            prompt.AppendLine("1. User-friendly names and descriptions for each table and column");
            prompt.AppendLine("2. Identification of potential naming conflicts or ambiguities");
            prompt.AppendLine("3. Suggested relationships that might be missing from the schema");
            prompt.AppendLine("4. Identification of unclear or technical names that should be improved");

            prompt.AppendLine("\nDatabase Schema:");
            prompt.AppendLine(schema);

            prompt.AppendLine("\nRespond with a JSON object having the following structure:");
            prompt.AppendLine("```json");
            prompt.AppendLine("{");
            prompt.AppendLine("  \"tableDescriptions\": [");
            prompt.AppendLine("    {");
            prompt.AppendLine("      \"tableName\": \"table_name\",");
            prompt.AppendLine("      \"suggestedName\": \"User Friendly Table Name\",");
            prompt.AppendLine("      \"suggestedDescription\": \"Clear description of what this table represents\"");
            prompt.AppendLine("    }");
            prompt.AppendLine("  ],");
            prompt.AppendLine("  \"columnDescriptions\": [");
            prompt.AppendLine("    {");
            prompt.AppendLine("      \"tableName\": \"table_name\",");
            prompt.AppendLine("      \"columnName\": \"column_name\",");
            prompt.AppendLine("      \"suggestedName\": \"User Friendly Column Name\",");
            prompt.AppendLine("      \"suggestedDescription\": \"Clear description of what this column represents\",");
            prompt.AppendLine("      \"isLookupColumn\": true/false");
            prompt.AppendLine("    }");
            prompt.AppendLine("  ],");
            prompt.AppendLine("  \"potentialConflicts\": [");
            prompt.AppendLine("    {");
            prompt.AppendLine("      \"type\": \"Table\", // or \"Column\"");
            prompt.AppendLine("      \"conflictDescription\": \"Description of the conflict\",");
            prompt.AppendLine("      \"items\": [");
            prompt.AppendLine("        {");
            prompt.AppendLine("          \"name\": \"conflicting_name\",");
            prompt.AppendLine("          \"tableName\": \"table_name\", // only for columns");
            prompt.AppendLine("          \"suggestedResolution\": \"Suggested way to resolve the conflict\"");
            prompt.AppendLine("        }");
            prompt.AppendLine("      ]");
            prompt.AppendLine("    }");
            prompt.AppendLine("  ],");
            //prompt.AppendLine("  \"suggestedRelationships\": [");
            //prompt.AppendLine("    {");
            //prompt.AppendLine("      \"sourceTable\": \"source_table\",");
            //prompt.AppendLine("      \"sourceColumn\": \"source_column\",");
            //prompt.AppendLine("      \"targetTable\": \"target_table\",");
            //prompt.AppendLine("      \"targetColumn\": \"target_column\",");
            //prompt.AppendLine("      \"relationshipType\": \"OneToMany\", // or \"ManyToOne\", \"OneToOne\", \"ManyToMany\"");
            //prompt.AppendLine("      \"confidence\": 0.85, // confidence score between 0 and 1");
            //prompt.AppendLine("      \"reasoning\": \"Explanation for why this relationship might exist\"");
            //prompt.AppendLine("    }");
            //prompt.AppendLine("  ],");
            //prompt.AppendLine("  \"unclearElements\": [");
            //prompt.AppendLine("    {");
            //prompt.AppendLine("      \"type\": \"Table\", // or \"Column\"");
            //prompt.AppendLine("      \"name\": \"element_name\",");
            //prompt.AppendLine("      \"tableName\": \"table_name\", // only for columns");
            //prompt.AppendLine("      \"issue\": \"Description of the clarity issue\",");
            //prompt.AppendLine("      \"suggestion\": \"Suggested improvement\"");
            //prompt.AppendLine("    }");
            //prompt.AppendLine("  ]");
            //prompt.AppendLine("}");
            prompt.AppendLine("```");

            prompt.AppendLine("\nGuidelines:");
            prompt.AppendLine("1. Provide business-oriented, non-technical friendly names and descriptions");
            prompt.AppendLine("2. For lookup columns (foreign keys), indicate isLookupColumn as true");
            prompt.AppendLine("3. Identify naming conflicts where similar names between tables or columns in same table might cause confusion");
           // prompt.AppendLine("4. Suggest logical relationships based on column names and data types");
            prompt.AppendLine("5. Focus on clarity and usability for non-technical users");
            prompt.AppendLine("6. Keep your response in pure JSON format with no additional text");

            return prompt.ToString();
        }

        /// <summary>
        /// Parses LLM response into a SchemaAnalysisResult object
        /// </summary>
        private SchemaAnalysisResult ParseLLMSchemaAnalysisResponse(string llmResponse)
        {
            try
            {
                // Store the raw response for reference
                var result = new SchemaAnalysisResult
                {
                    Success = true,
                    RawLLMResponse = llmResponse,
                    AnalysisData = new SchemaAnalysisData()
                };

                // Extract JSON from response
                var jsonStart = llmResponse.IndexOf('{');
                var jsonEnd = llmResponse.LastIndexOf('}');

                if (jsonStart >= 0 && jsonEnd > jsonStart)
                {
                    var json = llmResponse.Substring(jsonStart, jsonEnd - jsonStart + 1);

                    // Parse the JSON
                    var analysisData = JsonSerializer.Deserialize<SchemaAnalysisData>(
                        json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    if (analysisData != null)
                    {
                        result.AnalysisData = analysisData;
                    }
                    else
                    {
                        result.Success = false;
                        result.ErrorMessage = "Failed to parse LLM response data";
                    }
                }
                else
                {
                    result.Success = false;
                    result.ErrorMessage = "Invalid LLM response format. Expected JSON object.";
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing LLM response");
                return new SchemaAnalysisResult
                {
                    Success = false,
                    ErrorMessage = $"Error parsing LLM response: {ex.Message}",
                    RawLLMResponse = llmResponse
                };
            }
        }

        /// <summary>
        /// Applies analysis results to update the database schema
        /// </summary>
        public async Task<bool> ApplySchemaAnalysisResultsAsync(int databaseId, SchemaAnalysisData analysisData)
        {
            try
            {
                // Get database schema
                var database = await _databaseService.GetDatabaseByIdAsync(databaseId);
                if (database == null)
                {
                    return false;
                }

                // Get existing schema
                var schemaObj = await _schemaService.GetJsonSchemaByDataBaseIdAsync(databaseId);
                if (schemaObj == null)
                {
                    return false;
                }

                // Deserialize existing schema
                var schema = _schemaService.DeserializeSchema(schemaObj.SchemaData);
                if (schema == null)
                {
                    return false;
                }

                // Apply table descriptions
                if (analysisData.TableDescriptions != null)
                {
                    foreach (var tableDesc in analysisData.TableDescriptions)
                    {
                        var table = schema.Tables.Find(t => t.DBName.Equals(tableDesc.TableName, StringComparison.OrdinalIgnoreCase));
                        if (table != null)
                        {
                            table.FriendlyName = tableDesc.SuggestedName;
                            table.Description = tableDesc.SuggestedDescription;
                        }
                    }
                }

                // Apply column descriptions
                if (analysisData.ColumnDescriptions != null)
                {
                    foreach (var colDesc in analysisData.ColumnDescriptions)
                    {
                        var table = schema.Tables.Find(t => t.DBName.Equals(colDesc.TableName, StringComparison.OrdinalIgnoreCase));
                        if (table != null)
                        {
                            var column = table.Columns.Find(c => c.DBName.Equals(colDesc.ColumnName, StringComparison.OrdinalIgnoreCase));
                            if (column != null)
                            {
                                column.FriendlyName = colDesc.SuggestedName;
                                column.Description = colDesc.SuggestedDescription;
                                column.IsLookup = colDesc.IsLookupColumn;
                            }
                        }
                    }
                }

                // Serialize updated schema
                var updatedSchemaJson = _schemaService.SerializeSchema(schema);

                // Update schema in database
                schemaObj.SchemaData = updatedSchemaJson;
                schemaObj.ModifiedAt = DateTime.UtcNow;

                await _schemaService.UpdateSchemaAsync(schemaObj);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error applying schema analysis results for database ID: {databaseId}");
                return false;
            }
        }
    }
}