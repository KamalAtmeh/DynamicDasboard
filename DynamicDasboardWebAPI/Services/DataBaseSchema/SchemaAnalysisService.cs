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
        private readonly DatabaseSchemaService objDBSchemaService;
        private readonly DatabaseService _databaseService;
        private readonly LLMServiceFactory _llmServiceFactory;
        private readonly ILLMService _llmService;

        public SchemaAnalysisService(
            DatabaseSchemaService schemaService,
            DatabaseService databaseService,
            LLMServiceFactory llmServiceFactory)
        {
            objDBSchemaService = schemaService ?? throw new ArgumentNullException(nameof(schemaService));
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _llmServiceFactory = llmServiceFactory ?? throw new ArgumentNullException(nameof(llmServiceFactory));
          

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
                

                // Get database schema
                var database = await _databaseService.GetDatabaseByIdAsync(databaseId);
                if (database == null)
                {
                   
                    return new SchemaAnalysisResult
                    {
                        Success = false,
                        ErrorMessage = $"Database with ID {databaseId} not found"
                    };
                }

                // Get existing schema if available
                var schemaObj = await objDBSchemaService.GetSchemaObject(databaseId);
                if (schemaObj == null || string.IsNullOrEmpty(schemaObj.SchemaData))
                {
                  
                    schemaObj = await objDBSchemaService.GenerateAndGetDatabaseSchemaFromConnectedDBAsync(databaseId, database);

                    if (schemaObj == null)
                    {
                        return new SchemaAnalysisResult
                        {
                            Success = false,
                            ErrorMessage = "Failed to generate schema from database"
                        };
                    }
                }

                // Format schema for LLM analysis
                var schemaForAnalysis = objDBSchemaService.BuildOptimizedSchemaString(schemaObj);

                // Call LLM to analyze schema
                var analysisResult = await AnalyzeSchemaWithLLMAsync(schemaForAnalysis, database.Name);

                return analysisResult;
            }
            catch (Exception ex)
            {
                
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
                return ParseSchemaAnalysisResponse(response);
            }
            catch (Exception ex)
            {
               
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
            //prompt.AppendLine("  \"potentialConflicts\": [");
            //prompt.AppendLine("    {");
            //prompt.AppendLine("      \"type\": \"Table\", // or \"Column\"");
            //prompt.AppendLine("      \"conflictDescription\": \"Description of the conflict\",");
            //prompt.AppendLine("      \"items\": [");
            //prompt.AppendLine("        {");
            //prompt.AppendLine("          \"name\": \"conflicting_name\",");
            //prompt.AppendLine("          \"tableName\": \"table_name\", // only for columns");
            //prompt.AppendLine("          \"suggestedResolution\": \"Suggested way to resolve the conflict\"");
            //prompt.AppendLine("        }");
            //prompt.AppendLine("      ]");
            //prompt.AppendLine("    }");
            //prompt.AppendLine("  ],");
            prompt.AppendLine("  \"suggestedRelationships\": [");
            prompt.AppendLine("    {");
            prompt.AppendLine("      \"sourceTable\": \"source_table\",");
            prompt.AppendLine("      \"sourceColumn\": \"source_column\",");
            prompt.AppendLine("      \"targetTable\": \"target_table\",");
            prompt.AppendLine("      \"targetColumn\": \"target_column\",");
            prompt.AppendLine("      \"relationshipType\": \"OneToMany\", // or \"ManyToOne\", \"OneToOne\", \"ManyToMany\"");
            //prompt.AppendLine("      \"confidence\": 0.85, // confidence score between 0 and 1");
            prompt.AppendLine("      \"reasoning\": \"Explanation for why this relationship might exist\"");
            prompt.AppendLine("    }");
            prompt.AppendLine("  ],");
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
            //prompt.AppendLine("3. Identify naming conflicts where similar names between tables or columns in same table might cause confusion");
            prompt.AppendLine("4. Suggest logical relationships based on column names and data types up to 20 relation");
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
                
                return new SchemaAnalysisResult
                {
                    Success = false,
                    ErrorMessage = $"Error parsing LLM response: {ex.Message}",
                    RawLLMResponse = llmResponse
                };
            }
        }

        private SchemaAnalysisResult ParseSchemaAnalysisResponse(string response)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(response))
                {
                    return new SchemaAnalysisResult
                    {
                        Success = false,
                        ErrorMessage = "Empty response from LLM"
                    };
                }

                // Clean the response (remove markdown, find JSON)
                var cleanedJson = CleanJsonResponse(response);

                // Parse using LLM DTO (flat structure)
                var llmResponse = JsonSerializer.Deserialize<LlmSchemaAnalysisResponse>(
                    cleanedJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (llmResponse == null)
                {
                    return new SchemaAnalysisResult
                    {
                        Success = false,
                        ErrorMessage = "Failed to parse LLM response"
                    };
                }

                // Map to our model structure
                var analysisData = new SchemaAnalysisData
                {
                    TableDescriptions = llmResponse.TableDescriptions ?? new List<TableDescription>(),
                    ColumnDescriptions = llmResponse.ColumnDescriptions ?? new List<ColumnDescription>(),
                    PotentialConflicts = llmResponse.PotentialConflicts ?? new List<PotentialConflict>(),
                    UnclearElements = llmResponse.UnclearElements ?? new List<UnclearElement>(),

                    // Map flat relationships to nested structure
                    SuggestedRelationships = MapLlmRelationships(llmResponse.SuggestedRelationships)
                };

                return new SchemaAnalysisResult
                {
                    Success = true,
                    AnalysisData = analysisData
                };
            }
            catch (JsonException ex)
            {
                return new SchemaAnalysisResult
                {
                    Success = false,
                    ErrorMessage = $"JSON parsing error: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                return new SchemaAnalysisResult
                {
                    Success = false,
                    ErrorMessage = $"Unexpected error: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Maps flat LLM relationship structure to nested model structure
        /// </summary>
        private List<SuggestedRelationship> MapLlmRelationships(List<LlmSuggestedRelationship> llmRelationships)
        {
            if (llmRelationships == null || !llmRelationships.Any())
                return new List<SuggestedRelationship>();

            return llmRelationships.Select(lr => new SuggestedRelationship
            {
                RelationshipType = lr.RelationshipType ?? "ManyToOne",
                Confidence = lr.Confidence > 0 ? lr.Confidence : 0.9,
                Reasoning = lr.Reasoning,
                SourceTable = new RelationshipDetails
                {
                    TableName = lr.SourceTable,
                    ColumnName = lr.SourceColumn
                },
                TargetTable = new RelationshipDetails
                {
                    TableName = lr.TargetTable,
                    ColumnName = lr.TargetColumn
                }
            }).ToList();
        }

        /// <summary>
        /// Cleans LLM response by removing markdown and extracting JSON
        /// </summary>
        private string CleanJsonResponse(string response)
        {
            if (string.IsNullOrWhiteSpace(response))
                return "{}";

            var cleaned = response.Trim();

            // Remove markdown code blocks
            if (cleaned.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
                cleaned = cleaned.Substring(7);
            else if (cleaned.StartsWith("```"))
                cleaned = cleaned.Substring(3);

            if (cleaned.EndsWith("```"))
                cleaned = cleaned.Substring(0, cleaned.Length - 3);

            cleaned = cleaned.Trim();

            // Find JSON boundaries
            int start = cleaned.IndexOf('{');
            int end = cleaned.LastIndexOf('}');

            if (start >= 0 && end > start)
                return cleaned.Substring(start, end - start + 1);

            return cleaned;
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
                var schema = await objDBSchemaService.GetSchemaObject(databaseId);
                if (schema == null)
                {
                    return false;
                }

                // Deserialize existing schema
                

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
                var updatedSchemaJson = objDBSchemaService.SerializeSchema(schema);

                // Update schema in database
                schema.SchemaData = updatedSchemaJson;
                schema.ModifiedAt = DateTime.UtcNow;

                await objDBSchemaService.UpdateSchemaAsync(schema);

                return true;
            }
            catch (Exception ex)
            {
                
                return false;
            }
        }

        // In DynamicDasboardWebAPI/Services/DataBaseSchema/SchemaAnalysisService.cs
        public async Task<List<TermMapping>> SuggestTermMappingsAsync(int databaseId)
        {
            try
            {
                

                // Get database schema
                var schemaObj = await objDBSchemaService.GetSchemaObject(databaseId);
                if (schemaObj == null)
                {
                    
                    return new List<TermMapping>();
                }

                // Create optimized schema for LLM
                var schemaForLlm = objDBSchemaService.BuildOptimizedSchemaString(schemaObj);

                // Build prompt for LLM
                var prompt = BuildTermSuggestionPrompt(schemaForLlm);

                // Call LLM service
                var llmService = _llmServiceFactory.CreateLlmService();
                var response = await llmService.GenerateTermSuggestionsAsync(prompt);

                // Parse response into term mappings
                var termMappings = ParseTermSuggestionResponse(response, schemaObj);

                return termMappings;
            }
            catch (Exception ex)
            {
               
                return new List<TermMapping>();
            }
        }

        private string BuildTermSuggestionPrompt(string schema)
        {
            var prompt = new StringBuilder();

            prompt.AppendLine("You are an expert database analyst and business intelligence specialist.");
            prompt.AppendLine("Based on the database schema below, suggest up to 20 business terms that users might search for.");
            prompt.AppendLine("Think about common business concepts, metrics, and language that relates to this data model.");

            prompt.AppendLine("\nFor each term, provide:");
            prompt.AppendLine("1. The business term itself (what users might ask for)");
            prompt.AppendLine("2. A clear description of the term");
            prompt.AppendLine("3. The type of mapping (DirectColumn, CalculatedField, Aggregate, or FilterCondition)");
            prompt.AppendLine("4. The table and column name(s) related to this term");
            prompt.AppendLine("5. A formula or SQL expression (for calculated fields)");
            prompt.AppendLine("6. Related synonyms users might use");

            prompt.AppendLine("\nDatabase schema:");
            prompt.AppendLine(schema);

            prompt.AppendLine("\nRespond with a JSON array of term suggestions in this format:");
            prompt.AppendLine("[");
            prompt.AppendLine("  {");
            prompt.AppendLine("    \"businessTerm\": \"Gross Margin\",");
            prompt.AppendLine("    \"description\": \"The difference between revenue and cost of goods sold, expressed as a percentage of revenue\",");
            prompt.AppendLine("    \"type\": \"CalculatedField\",");
            prompt.AppendLine("    \"dependencies\": [");
            prompt.AppendLine("      { \"tableName\": \"Sales\", \"columnName\": \"Revenue\" },");
            prompt.AppendLine("      { \"tableName\": \"Sales\", \"columnName\": \"Cost\" }");
            prompt.AppendLine("    ],");
            prompt.AppendLine("    \"formula\": \"((Revenue - Cost) / Revenue) * 100\",");
            prompt.AppendLine("    \"synonyms\": [\"profit margin\", \"margin percentage\", \"profit percentage\"]");
            prompt.AppendLine("  },");
            prompt.AppendLine("  {");
            prompt.AppendLine("    \"businessTerm\": \"Active Customer\",");
            prompt.AppendLine("    \"description\": \"A customer with at least one purchase in the last 90 days\",");
            prompt.AppendLine("    \"type\": \"FilterCondition\",");
            prompt.AppendLine("    \"dependencies\": [");
            prompt.AppendLine("      { \"tableName\": \"Customers\", \"columnName\": \"CustomerID\" },");
            prompt.AppendLine("      { \"tableName\": \"Orders\", \"columnName\": \"OrderDate\" }");
            prompt.AppendLine("    ],");
            prompt.AppendLine("    \"filterCondition\": \"EXISTS (SELECT 1 FROM Orders WHERE Orders.CustomerID = Customers.CustomerID AND Orders.OrderDate >= DATEADD(day, -90, GETDATE()))\",");
            prompt.AppendLine("    \"synonyms\": [\"current customer\", \"recent buyer\"]");
            prompt.AppendLine("  }");
            prompt.AppendLine("]");

            return prompt.ToString();
        }

        private List<TermMapping> ParseTermSuggestionResponse(string response, DatabaseSchema schema)
        {
            var termMappings = new List<TermMapping>();

            try
            {
                // Extract JSON from response
                int jsonStart = response.IndexOf('[');
                int jsonEnd = response.LastIndexOf(']') + 1;

                if (jsonStart >= 0 && jsonEnd > jsonStart)
                {
                    var json = response.Substring(jsonStart, jsonEnd - jsonStart);
                    var suggestions = JsonSerializer.Deserialize<List<TermSuggestion>>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (suggestions != null)
                    {
                        foreach (var suggestion in suggestions)
                        {
                            var mapping = new TermMapping
                            {
                                BusinessTerm = suggestion.BusinessTerm,
                                Description = suggestion.Description,
                                Type = Enum.Parse<TermMappingType>(suggestion.Type),
                                Synonyms = suggestion.Synonyms ?? new List<string>(),
                                IsLLMSuggested = true
                            };

                            // Process dependencies and map to actual schema IDs
                            if (suggestion.Dependencies != null)
                            {
                                mapping.Dependencies = new List<TermMappingDependency>();

                                foreach (var dep in suggestion.Dependencies)
                                {
                                    var tableObj = schema.Tables.FirstOrDefault(t =>
                                        t.DBName.Equals(dep.TableName, StringComparison.OrdinalIgnoreCase));

                                    if (tableObj != null)
                                    {
                                        var columnObj = tableObj.Columns?.FirstOrDefault(c =>
                                            c.DBName.Equals(dep.ColumnName, StringComparison.OrdinalIgnoreCase));

                                        if (columnObj != null)
                                        {
                                            mapping.Dependencies.Add(new TermMappingDependency
                                            {
                                                TableId = tableObj.ID,
                                                ColumnId = columnObj.ID,
                                                TableName = tableObj.DBName,
                                                ColumnName = columnObj.DBName
                                            });

                                            // If this is a direct column mapping, set the table and column
                                            if (mapping.Type == TermMappingType.DirectColumn &&
                                                string.IsNullOrEmpty(mapping.TableId))
                                            {
                                                mapping.TableId = tableObj.ID;
                                                mapping.ColumnId = columnObj.ID;
                                            }
                                        }
                                    }
                                }
                            }

                            // Set formula or filter condition
                            if (mapping.Type == TermMappingType.CalculatedField)
                            {
                                mapping.Formula = suggestion.Formula;
                            }
                            else if (mapping.Type == TermMappingType.FilterCondition)
                            {
                                mapping.FilterCondition = suggestion.FilterCondition;
                            }

                            termMappings.Add(mapping);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }

            return termMappings;
        }

        /// <summary>
        /// Saves term mappings to a database schema
        /// </summary>
        /// <param name="databaseId">The ID of the database</param>
        /// <param name="termMappings">The term mappings to save</param>
        /// <returns>True if successful, false otherwise</returns>
        

        #region Table Analysis

        public async Task<SchemaAnalysisResult> AnalyzeTablesAsync(int databaseId, string schemaString)
        {
            try
            {


                // Create a specialized prompt for table analysis
                var prompt = BuildTableAnalysisPrompt(schemaString);

                // Call LLM with focused prompt
                var response = await _llmService.GenerateSchemaAnalysisAsync(prompt);

                // Parse the result focusing only on table descriptions
                var result = ParseTableAnalysisResponse(response);

                return result;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private string BuildTableAnalysisPrompt(string schema)
        {
            var prompt = new StringBuilder();

            prompt.AppendLine("You are an expert database analyst helping improve the usability of database tables.");
            prompt.AppendLine("\nYour task is to analyze ONLY the tables in the schema below and provide:");
            prompt.AppendLine("1. User-friendly names for each table");
            prompt.AppendLine("2. Clear descriptions of what each table represents in business terms");

            prompt.AppendLine("\nDatabase Schema:");
            prompt.AppendLine(schema);

            prompt.AppendLine("\nRespond with a JSON object having the following structure:");
            prompt.AppendLine("{");
            prompt.AppendLine("  \"tableDescriptions\": [");
            prompt.AppendLine("    {");
            prompt.AppendLine("      \"tableName\": \"table_name\",");
            prompt.AppendLine("      \"suggestedName\": \"User Friendly Table Name\",");
            prompt.AppendLine("      \"suggestedDescription\": \"Clear description of what this table represents\"");
            prompt.AppendLine("    }");
            prompt.AppendLine("  ]");
            prompt.AppendLine("}");

            prompt.AppendLine("\nGuidelines:");
            prompt.AppendLine("1. Provide business-oriented, non-technical friendly names and descriptions");
            prompt.AppendLine("2. Focus on clarity and usability for non-technical users");
            prompt.AppendLine("3. Keep your response in pure JSON format with no additional text");

            return prompt.ToString();
        }

        private SchemaAnalysisResult ParseTableAnalysisResponse(string response)
        {
            try
            {
                // Extract JSON from response
                var jsonStart = response.IndexOf('{');
                var jsonEnd = response.LastIndexOf('}');

                if (jsonStart >= 0 && jsonEnd > jsonStart)
                {
                    var json = response.Substring(jsonStart, jsonEnd - jsonStart + 1);

                    // Deserialize directly to TableDescriptionsResponse
                    var parsedResponse = JsonSerializer.Deserialize<TableDescriptionsResponse>(
                        json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    if (parsedResponse?.TableDescriptions != null)
                    {
                        // Create result with only table descriptions
                        return new SchemaAnalysisResult
                        {
                            Success = true,
                            AnalysisData = new SchemaAnalysisData
                            {
                                TableDescriptions = parsedResponse.TableDescriptions,
                                // Empty lists for other properties
                                ColumnDescriptions = new List<ColumnDescription>(),
                                PotentialConflicts = new List<PotentialConflict>(),
                                SuggestedRelationships = new List<SuggestedRelationship>(),
                                UnclearElements = new List<UnclearElement>()
                            },
                            RawLLMResponse = response
                        };
                    }
                }

                return new SchemaAnalysisResult
                {
                    Success = false,
                    ErrorMessage = "Failed to parse table analysis response",
                    RawLLMResponse = response
                };
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private class TableDescriptionsResponse
        {
            public List<TableDescription> TableDescriptions { get; set; }
        }

        #endregion

        #region Column Analysis

        public async Task<SchemaAnalysisResult> AnalyzeColumnsAsync(int databaseId, string schemaString)
        {
            try
            {


                // Create a specialized prompt for column analysis
                var prompt = BuildColumnAnalysisPrompt(schemaString);

                // Call LLM with focused prompt
                var response = await _llmService.GenerateSchemaAnalysisAsync(prompt);

                // Parse the result focusing only on column descriptions
                var result = ParseColumnAnalysisResponse(response);

                return result;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private string BuildColumnAnalysisPrompt(string schema)
        {
            var prompt = new StringBuilder();

            prompt.AppendLine("You are an expert database analyst helping improve the usability of database columns.");
            prompt.AppendLine("\nYour task is to analyze ONLY the columns in the schema below and provide:");
            prompt.AppendLine("1. User-friendly names for each column");
            prompt.AppendLine("2. Clear descriptions of what each column represents in business terms");
            prompt.AppendLine("3. Identification of lookup columns (foreign keys or reference data)");

            prompt.AppendLine("\nDatabase Schema:");
            prompt.AppendLine(schema);

            prompt.AppendLine("\nRespond with a JSON object having the following structure:");
            prompt.AppendLine("{");
            prompt.AppendLine("  \"columnDescriptions\": [");
            prompt.AppendLine("    {");
            prompt.AppendLine("      \"tableName\": \"table_name\",");
            prompt.AppendLine("      \"columnName\": \"column_name\",");
            prompt.AppendLine("      \"suggestedName\": \"User Friendly Column Name\",");
            prompt.AppendLine("      \"suggestedDescription\": \"Clear description of what this column represents\",");
            prompt.AppendLine("      \"isLookupColumn\": true/false");
            prompt.AppendLine("    }");
            prompt.AppendLine("  ]");
            prompt.AppendLine("}");

            prompt.AppendLine("\nGuidelines:");
            prompt.AppendLine("1. Provide business-oriented, non-technical friendly names and descriptions");
            prompt.AppendLine("2. Set isLookupColumn to true for foreign keys and reference data columns");
            prompt.AppendLine("3. Focus on clarity and usability for non-technical users");
            prompt.AppendLine("4. Keep your response in pure JSON format with no additional text");

            return prompt.ToString();
        }

        private SchemaAnalysisResult ParseColumnAnalysisResponse(string response)
        {
            try
            {
                // Extract JSON from response
                var jsonStart = response.IndexOf('{');
                var jsonEnd = response.LastIndexOf('}');

                if (jsonStart >= 0 && jsonEnd > jsonStart)
                {
                    var json = response.Substring(jsonStart, jsonEnd - jsonStart + 1);

                    // Deserialize directly to ColumnDescriptionsResponse
                    var parsedResponse = JsonSerializer.Deserialize<ColumnDescriptionsResponse>(
                        json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    if (parsedResponse?.ColumnDescriptions != null)
                    {
                        // Create result with only column descriptions
                        return new SchemaAnalysisResult
                        {
                            Success = true,
                            AnalysisData = new SchemaAnalysisData
                            {
                                ColumnDescriptions = parsedResponse.ColumnDescriptions,
                                // Empty lists for other properties
                                TableDescriptions = new List<TableDescription>(),
                                PotentialConflicts = new List<PotentialConflict>(),
                                SuggestedRelationships = new List<SuggestedRelationship>(),
                                UnclearElements = new List<UnclearElement>()
                            },
                            RawLLMResponse = response
                        };
                    }
                }

                return new SchemaAnalysisResult
                {
                    Success = false,
                    ErrorMessage = "Failed to parse column analysis response",
                    RawLLMResponse = response
                };
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private class ColumnDescriptionsResponse
        {
            public List<ColumnDescription> ColumnDescriptions { get; set; }
        }

        #endregion

        #region Relationship Analysis

        public async Task<SchemaAnalysisResult> AnalyzeRelationshipsAsync(int databaseId, string schemaString)
        {
            try
            {


                // Create a specialized prompt for relationship analysis
                var prompt = BuildRelationshipAnalysisPrompt(schemaString);

                // Call LLM with focused prompt
                var response = await _llmService.GenerateSchemaAnalysisAsync(prompt);

                // Parse the result focusing only on suggested relationships
                var result = ParseRelationshipAnalysisResponse(response);

                return result;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private string BuildRelationshipAnalysisPrompt(string schema)
        {
            var prompt = new StringBuilder();

            prompt.AppendLine("You are an expert database analyst helping identify relationships between tables.");
            prompt.AppendLine("\nYour task is to analyze the schema below and suggest relationships that might be missing:");
            prompt.AppendLine("1. Identify potential relationships based on column names, data types, and conventional naming patterns");
            prompt.AppendLine("2. Suggest relationship types (OneToOne, OneToMany, ManyToOne, ManyToMany)");
            prompt.AppendLine("3. Provide confidence scores and reasoning for each suggestion");

            prompt.AppendLine("\nDatabase Schema:");
            prompt.AppendLine(schema);

            prompt.AppendLine("\nRespond with a JSON object having the following structure:");
            prompt.AppendLine("{");
            prompt.AppendLine("  \"suggestedRelationships\": [");
            prompt.AppendLine("    {");
            prompt.AppendLine("      \"sourceTable\": \"source_table\",");
            prompt.AppendLine("      \"sourceColumn\": \"source_column\",");
            prompt.AppendLine("      \"targetTable\": \"target_table\",");
            prompt.AppendLine("      \"targetColumn\": \"target_column\",");
            prompt.AppendLine("      \"relationshipType\": \"OneToMany\",");
            prompt.AppendLine("      \"confidence\": 0.85,");
            prompt.AppendLine("      \"reasoning\": \"Explanation for why this relationship might exist\"");
            prompt.AppendLine("    }");
            prompt.AppendLine("  ]");
            prompt.AppendLine("}");

            prompt.AppendLine("\nGuidelines:");
            prompt.AppendLine("1. Focus on discovering relationships that might not already be explicitly defined");
            prompt.AppendLine("2. Provide confidence scores between 0 and 1");
            prompt.AppendLine("3. Only suggest relationships with confidence score >= 0.6");
            prompt.AppendLine("4. Keep your response in pure JSON format with no additional text");

            return prompt.ToString();
        }

        private SchemaAnalysisResult ParseRelationshipAnalysisResponse(string response)
        {
            try
            {
                // Extract JSON from response
                var jsonStart = response.IndexOf('{');
                var jsonEnd = response.LastIndexOf('}');

                if (jsonStart >= 0 && jsonEnd > jsonStart)
                {
                    var json = response.Substring(jsonStart, jsonEnd - jsonStart + 1);

                    // Deserialize directly to RelationshipResponse
                    var parsedResponse = JsonSerializer.Deserialize<RelationshipResponse>(
                        json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    if (parsedResponse?.SuggestedRelationships != null)
                    {
                        // Create result with only suggested relationships
                        return new SchemaAnalysisResult
                        {
                            Success = true,
                            AnalysisData = new SchemaAnalysisData
                            {
                                SuggestedRelationships = parsedResponse.SuggestedRelationships,
                                // Empty lists for other properties
                                TableDescriptions = new List<TableDescription>(),
                                ColumnDescriptions = new List<ColumnDescription>(),
                                PotentialConflicts = new List<PotentialConflict>(),
                                UnclearElements = new List<UnclearElement>()
                            },
                            RawLLMResponse = response
                        };
                    }
                }

                return new SchemaAnalysisResult
                {
                    Success = false,
                    ErrorMessage = "Failed to parse relationship analysis response",
                    RawLLMResponse = response
                };
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private class RelationshipResponse
        {
            public List<SuggestedRelationship> SuggestedRelationships { get; set; }
        }

        #endregion

        #region Conflict Analysis

        public async Task<SchemaAnalysisResult> AnalyzeConflictsAsync(int databaseId, string schemaString)
        {
            try
            {
                // Create a specialized prompt for conflict analysis
                var prompt = BuildConflictAnalysisPrompt(schemaString);

                // Call LLM with focused prompt
                var response = await _llmService.GenerateSchemaAnalysisAsync(prompt);

                // Parse the result focusing only on potential conflicts
                var result = ParseConflictAnalysisResponse(response);

                return result;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private string BuildConflictAnalysisPrompt(string schema)
        {
            var prompt = new StringBuilder();

            prompt.AppendLine("You are an expert database analyst helping identify naming conflicts and ambiguities.");
            prompt.AppendLine("\nYour task is to analyze the schema below and identify potential naming conflicts or ambiguities:");
            prompt.AppendLine("1. Identify similar table names that might cause confusion");
            prompt.AppendLine("2. Identify similar column names used differently across tables");
            prompt.AppendLine("3. Identify vague or unclear names that could be misinterpreted");
            prompt.AppendLine("4. Suggest resolutions for each identified conflict");

            prompt.AppendLine("\nDatabase Schema:");
            prompt.AppendLine(schema);

            prompt.AppendLine("\nRespond with a JSON object having the following structure:");
            prompt.AppendLine("{");
            prompt.AppendLine("  \"potentialConflicts\": [");
            prompt.AppendLine("    {");
            prompt.AppendLine("      \"type\": \"Table\",");
            prompt.AppendLine("      \"conflictDescription\": \"Description of the conflict\",");
            prompt.AppendLine("      \"items\": [");
            prompt.AppendLine("        {");
            prompt.AppendLine("          \"name\": \"conflicting_name\",");
            prompt.AppendLine("          \"tableName\": \"table_name\",");
            prompt.AppendLine("          \"suggestedResolution\": \"Suggested way to resolve the conflict\"");
            prompt.AppendLine("        }");
            prompt.AppendLine("      ]");
            prompt.AppendLine("    }");
            prompt.AppendLine("  ],");
            prompt.AppendLine("  \"unclearElements\": [");
            prompt.AppendLine("    {");
            prompt.AppendLine("      \"type\": \"Table\",");
            prompt.AppendLine("      \"name\": \"element_name\",");
            prompt.AppendLine("      \"tableName\": \"table_name\",");
            prompt.AppendLine("      \"issue\": \"Description of the clarity issue\",");
            prompt.AppendLine("      \"suggestion\": \"Suggested improvement\"");
            prompt.AppendLine("    }");
            prompt.AppendLine("  ]");
            prompt.AppendLine("}");

            prompt.AppendLine("\nGuidelines:");
            prompt.AppendLine("1. For column conflicts, include the tableName field");
            prompt.AppendLine("2. For table conflicts, omit the tableName field");
            prompt.AppendLine("3. Focus on clarity and usability for non-technical users");
            prompt.AppendLine("4. Keep your response in pure JSON format with no additional text");

            return prompt.ToString();
        }

        private SchemaAnalysisResult ParseConflictAnalysisResponse(string response)
        {
            try
            {
                // Extract JSON from response
                var jsonStart = response.IndexOf('{');
                var jsonEnd = response.LastIndexOf('}');

                if (jsonStart >= 0 && jsonEnd > jsonStart)
                {
                    var json = response.Substring(jsonStart, jsonEnd - jsonStart + 1);

                    // Deserialize directly to ConflictResponse
                    var parsedResponse = JsonSerializer.Deserialize<ConflictResponse>(
                        json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    if (parsedResponse != null)
                    {
                        // Create result with only conflicts and unclear elements
                        return new SchemaAnalysisResult
                        {
                            Success = true,
                            AnalysisData = new SchemaAnalysisData
                            {
                                PotentialConflicts = parsedResponse.PotentialConflicts ?? new List<PotentialConflict>(),
                                UnclearElements = parsedResponse.UnclearElements ?? new List<UnclearElement>(),
                                // Empty lists for other properties
                                TableDescriptions = new List<TableDescription>(),
                                ColumnDescriptions = new List<ColumnDescription>(),
                                SuggestedRelationships = new List<SuggestedRelationship>()
                            },
                            RawLLMResponse = response
                        };
                    }
                }

                return new SchemaAnalysisResult
                {
                    Success = false,
                    ErrorMessage = "Failed to parse conflict analysis response",
                    RawLLMResponse = response
                };
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private class ConflictResponse
        {
            public List<PotentialConflict> PotentialConflicts { get; set; }
            public List<UnclearElement> UnclearElements { get; set; }
        }

        #endregion

        #region Term Mapping Analysis

        public async Task<Dictionary<string, string>> GenerateTermMappingsAsync(int databaseId, string schemaString)
        {
            try
            {

                // Create a specialized prompt for term mapping
                var prompt = BuildTermMappingPrompt(schemaString);

                // Call LLM with focused prompt
                var response = await _llmService.GenerateSchemaAnalysisAsync(prompt);

                // Parse the result to get term mappings
                var termMappings = ParseTermMappingResponse(response);

                return termMappings;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private string BuildTermMappingPrompt(string schema)
        {
            var prompt = new StringBuilder();

            prompt.AppendLine("You are an expert in translating technical database terms into business-friendly language.");
            prompt.AppendLine("\nYour task is to analyze the schema below and create mappings between technical terms and business terms:");
            prompt.AppendLine("1. Identify technical database terms (table names, column names)");
            prompt.AppendLine("2. Provide business-friendly equivalents for each term");
            prompt.AppendLine("3. Focus on terms that non-technical users would find confusing");

            prompt.AppendLine("\nDatabase Schema:");
            prompt.AppendLine(schema);

            prompt.AppendLine("\nRespond with a JSON object having the following structure:");
            prompt.AppendLine("{");
            prompt.AppendLine("  \"termMappings\": {");
            prompt.AppendLine("    \"technical_term\": \"business_term\",");
            prompt.AppendLine("    \"another_technical_term\": \"another_business_term\"");
            prompt.AppendLine("  }");
            prompt.AppendLine("}");

            prompt.AppendLine("\nGuidelines:");
            prompt.AppendLine("1. Include table and column names (e.g., \"customer_id\", \"Customers\")");
            prompt.AppendLine("2. Include common technical patterns (e.g., \"_id\", \"is_active\")");
            prompt.AppendLine("3. Use natural, conversational business terms");
            prompt.AppendLine("4. Keep your response in pure JSON format with no additional text");

            return prompt.ToString();
        }

        private Dictionary<string, string> ParseTermMappingResponse(string response)
        {
            try
            {
                // Extract JSON from response
                var jsonStart = response.IndexOf('{');
                var jsonEnd = response.LastIndexOf('}');

                if (jsonStart >= 0 && jsonEnd > jsonStart)
                {
                    var json = response.Substring(jsonStart, jsonEnd - jsonStart + 1);

                    // Deserialize directly to TermMappingResponse
                    var parsedResponse = JsonSerializer.Deserialize<TermMappingResponse>(
                        json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    if (parsedResponse?.TermMappings != null)
                    {
                        return parsedResponse.TermMappings;
                    }
                }

 
                return new Dictionary<string, string>();
            }
            catch (Exception ex)
            {

                throw;
            }
        }



        #endregion

        ///// <summary>
        ///// Saves term mappings for a database
        ///// </summary>
        //public async Task<bool> SaveTermMappingsAsync(int databaseId, List<TermMapping> termMappings)
        //{
        //    try
        //    {


        //        // Get the schema
        //        var schema = await objDBSchemaService.GetSchemaObject(databaseId);
        //        if (schema == null)
        //        {

        //            return false;
        //        }

        //        // Set or update TermMappings property
        //        schema.TermMappings = termMappings;

        //        // Update schema
        //        await objDBSchemaService.UpdateSchemaAsync(schema);

        //        return true;
        //    }
        //    catch (Exception ex)
        //    {

        //        return false;
        //    }
        //}


    }
}