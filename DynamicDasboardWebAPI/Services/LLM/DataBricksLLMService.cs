using DynamicDashboardCommon.Models;
using DynamicDashboardCommon.Models.LLM;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DynamicDasboardWebAPI.Services.LLM
{
    /// <summary>
    /// Implementation of ILLMService using Databricks API
    /// </summary>
    public class DatabricksLLMService : ILLMService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly string _apiToken;
        private readonly string _databricksHost;
        private readonly string _endpointName;
        private readonly string _modelName;
        private readonly int _timeoutSeconds;

        public DatabricksLLMService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            // Get configuration values
            _apiToken = _configuration["Databricks:ApiToken"]
                ?? throw new InvalidOperationException("Databricks API token not found in configuration");

            _databricksHost = _configuration["Databricks:Host"]
                ?? throw new InvalidOperationException("Databricks host not found in configuration");

            _endpointName = _configuration["Databricks:EndpointName"]
                ?? "databricks-meta-llama-3-70b-instruct";

            _modelName = _configuration["Databricks:ModelName"]
                ?? "meta-llama-3-70b-instruct";

            _timeoutSeconds = _configuration.GetValue<int>("LlmService:Timeout", 300);
        }

        /// <inheritdoc/>
        public async Task<SqlGenerationWithExplanationResponse> GenerateSqlWithExplanationAsync(
            string question,
            string databaseSchema,
            Dictionary<string, string> dbDescriptions = null)
        {
            try
            {
                // Build the system prompt
                var systemPrompt = BuildSQLScriptwithExplanationSystemPrompt(databaseSchema, dbDescriptions);

                // Build the user prompt
                var userPrompt = $"Question: {question}\n\nPlease generate a SQL query for this question based on the provided schema, and explain what it does in business terms.";

                // Call Databricks API
                var response = await CallDatabricksApiAsync(systemPrompt, userPrompt);

                // Parse the SQL explanation response
                return ParseSqlExplanationResponse(response, question);
            }
            catch (Exception ex)
            {
                return new SqlGenerationWithExplanationResponse
                {
                    Success = false,
                    ErrorMessage = $"Error in Databricks LLM service: {ex.Message}",
                    OriginalQuestion = question
                };
            }
        }

        /// <inheritdoc/>
        public async Task<ExplanationResponse> GenerateExplanationAsync(
            string question,
            string databaseSchema,
            Dictionary<string, string> adminDescriptions = null)
        {
            try
            {
                // Build the system prompt
                var systemPrompt = BuildExplanationSystemPrompt(databaseSchema, adminDescriptions);

                // Build the user prompt
                var userPrompt = $"Question: {question}\n\nPlease explain how you understand this question in user-friendly terms, " +
                    "identify any ambiguities, and list any adjustable parameters.";

                // Call Databricks API
                var response = await CallDatabricksApiAsync(systemPrompt, userPrompt);

                // Parse the explanation response
                return ParseExplanationResponse(response);
            }
            catch (Exception ex)
            {
                return new ExplanationResponse
                {
                    Explanation = $"Error generating explanation: {ex.Message}",
                    HasAmbiguities = false,
                    ConfidenceScore = 0
                };
            }
        }

        /// <inheritdoc/>
        public async Task<string> GenerateSqlAsync(
            string question,
            string confirmedUnderstanding,
            string databaseSchema,
            Dictionary<string, string> resolvedAmbiguities = null)
        {
            try
            {
                // Build the system prompt
                var systemPrompt = BuildSqlGenerationSystemPrompt(databaseSchema);

                // Build the user prompt with confirmed understanding and resolved ambiguities
                var userPrompt = new StringBuilder();
                userPrompt.AppendLine($"Original question: {question}");
                userPrompt.AppendLine($"Confirmed understanding: {confirmedUnderstanding}");

                if (resolvedAmbiguities != null && resolvedAmbiguities.Count > 0)
                {
                    userPrompt.AppendLine("\nResolved ambiguities:");
                    foreach (var ambiguity in resolvedAmbiguities)
                    {
                        userPrompt.AppendLine($"- {ambiguity.Key}: {ambiguity.Value}");
                    }
                }

                userPrompt.AppendLine("\nGenerate a SQL query that answers this question. Return ONLY the SQL without any explanation or formatting.");

                // Call Databricks API
                var response = await CallDatabricksApiAsync(systemPrompt, userPrompt.ToString());

                // Clean up the response (extract SQL)
                return ExtractSqlFromText(response);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error generating SQL: {ex.Message}", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<string> GenerateResultExplanationAsync(
            string question,
            string sql,
            List<Dictionary<string, object>> results)
        {
            try
            {
                // Build the system prompt
                var systemPrompt = "You are a helpful assistant explaining database query results to non-technical users. " +
                    "Provide clear, concise explanations that focus on the business insights from the data.";

                // Build the user prompt
                var userPrompt = new StringBuilder();
                userPrompt.AppendLine($"Original question: {question}");
                userPrompt.AppendLine($"SQL query used: {sql}");
                userPrompt.AppendLine("\nQuery results (first few rows):");

                // Add a sample of the results (up to 5 rows)
                var resultSample = results.Count <= 5 ? results : results.GetRange(0, 5);
                userPrompt.AppendLine(JsonSerializer.Serialize(resultSample, new JsonSerializerOptions { WriteIndented = true }));

                userPrompt.AppendLine($"\nTotal rows returned: {results.Count}");
                userPrompt.AppendLine("\nPlease provide a brief, user-friendly explanation of these results " +
                    "that highlights key insights and answers the original question. Keep it to 2-3 sentences.");

                // Call Databricks API
                var response = await CallDatabricksApiAsync(systemPrompt, userPrompt.ToString());

                // Return the explanation
                return response.Trim();
            }
            catch (Exception ex)
            {
                return $"Error generating result explanation: {ex.Message}";
            }
        }

        /// <inheritdoc/>
        public async Task<string> GenerateSchemaAnalysisAsync(string prompt)
        {
            try
            {
                // Use a generic system prompt for schema analysis
                var systemPrompt = "You are an expert database analyst helping improve the usability of database schemas.";

                // Call Databricks API with the prompt
                var response = await CallDatabricksApiAsync(systemPrompt, prompt);

                return response;
            }
            catch (Exception ex)
            {
                return $"Error generating schema analysis: {ex.Message}";
            }
        }

        /// <inheritdoc/>
        public async Task<string> GenerateTermSuggestionsAsync(string prompt)
        {
            try
            {
                // Use a generic system prompt for term suggestions
                var systemPrompt = "You are a terminology expert helping improve the clarity of database terms.";

                // Call Databricks API with the prompt
                var response = await CallDatabricksApiAsync(systemPrompt, prompt);

                return response;
            }
            catch (Exception ex)
            {
                return $"Error generating term suggestions: {ex.Message}";
            }
        }

        #region Private Helper Methods

        private string BuildSQLScriptwithExplanationSystemPrompt(string databaseSchema, Dictionary<string, string> adminDescriptions)
        {
            var prompt = new StringBuilder();
            prompt.AppendLine("You are an AI assistant that generates SQL queries from natural language questions and explains them in business terms. " +
                            "Your primary task is to analyze if the question is related to the database schema, and if so, create a SQL query and explain what it does.");

            // Primary approach and priorities
            prompt.AppendLine("\nFollowing this approach:");
            prompt.AppendLine("1. First, determine if the question is related to the provided database schema");
            prompt.AppendLine("2. If related, generate a SQL query and explain the business meaning");
            prompt.AppendLine("3. If not related (or partially unrelated), explain why and suggest alternative topics");
            prompt.AppendLine("4. Identify adjustable parameters and potential ambiguities in related questions");

            // Schema relevance analysis
            prompt.AppendLine("\nSchema Relevance Analysis:");
            prompt.AppendLine("- Carefully analyze if the question involves data that exists in the schema");
            prompt.AppendLine("- If the question is completely unrelated to the schema, set isSchemaRelated: false");
            prompt.AppendLine("- If only parts of the question are unrelated, set hasPartiallyUnrelatedContent: true");
            prompt.AppendLine("- For unrelated questions, identify 3-5 topics that the schema actually contains");
            prompt.AppendLine("- For unrelated questions, suggest 3 specific example questions related to the schema");
            prompt.AppendLine("- For partially unrelated questions, list the unrelated parts in the unrelatedQuestionParts array");

            // SQL generation requirements
            prompt.AppendLine("\nSQL Generation Rules (for related questions):");
            prompt.AppendLine("- Use only tables and columns that exist in the provided schema");
            prompt.AppendLine("- Ensure the SQL is compatible with the specified database type");
            prompt.AppendLine("- If no database type is specified, use ANSI-standard SQL");
            prompt.AppendLine("- If a specific database type is specified (MySQL, SQL Server, Oracle, PostgreSQL, etc.), optimize the SQL for that platform");
            prompt.AppendLine("- Qualify all column names with table aliases (e.g., users.name)");
            prompt.AppendLine("- Handle NULL values appropriately");
            prompt.AppendLine("- Ensure GROUP BY includes all non-aggregated columns");
            prompt.AppendLine("- Use only SELECT queries (no data modification)");
            prompt.AppendLine("- For pagination/row limiting, use the appropriate syntax for the specified database:");
            prompt.AppendLine("  * SQL Server: TOP n");
            prompt.AppendLine("  * MySQL/PostgreSQL/SQLite: LIMIT n");
            prompt.AppendLine("  * Oracle 12c+: FETCH FIRST n ROWS ONLY");
            prompt.AppendLine("  * Oracle before 12c: WHERE ROWNUM <= n");
            prompt.AppendLine("  * ANSI-standard: FETCH FIRST n ROWS ONLY");

            // Business explanation requirements
            prompt.AppendLine("\nBusiness Explanation Rules:");
            prompt.AppendLine("1. Use natural, conversational language focused on business meaning");
            prompt.AppendLine("2. Explain what data will be retrieved and any filters or conditions");
            prompt.AppendLine("3. Use defined descriptions instead of technical database terms");
            prompt.AppendLine("4. Highlight business insights the query provides");

            prompt.AppendLine("\nDatabase schema:");
            prompt.AppendLine(databaseSchema);

            if (adminDescriptions != null && adminDescriptions.Count > 0)
            {
                prompt.AppendLine("\nBusiness terminology (use these terms in your explanation instead of technical names):");
                foreach (var description in adminDescriptions)
                {
                    prompt.AppendLine($"- {description.Key}: {description.Value}");
                }
            }

            // Response format with added fields for schema relevance
            prompt.AppendLine("\nYour response should be structured as JSON with the following fields:");
            prompt.AppendLine("- isSchemaRelated: Boolean indicating if the question relates to the schema");
            prompt.AppendLine("- schemaRelevanceMessage: Explanation if the question is not related to the schema");
            prompt.AppendLine("- hasPartiallyUnrelatedContent: Boolean indicating if parts of the question are unrelated");
            prompt.AppendLine("- unrelatedQuestionParts: Array of question parts not related to the schema");
            prompt.AppendLine("- suggestedTopics: Array of topics the user can ask about (if question is unrelated)");
            prompt.AppendLine("- suggestedQuestions: Array of example questions related to the schema");
            prompt.AppendLine("- sqlQuery: The generated SQL query (only if question is related)");
            prompt.AppendLine("- businessExplanation: A user-friendly explanation of what the query does");
            prompt.AppendLine("- dbType: The database type this SQL is optimized for");
            prompt.AppendLine("- dbNotes: Notes about database compatibility or adaptation requirements");
            prompt.AppendLine("- hasAmbiguities: Boolean indicating if any ambiguities were detected");
            prompt.AppendLine("- detectedAmbiguities: Dictionary of ambiguous terms and their possible interpretations");
            prompt.AppendLine("- adjustableParameters: Dictionary of parameters that could be adjusted");
            prompt.AppendLine("- termMapping: Dictionary mapping technical terms to friendly terms (from descriptions) used");

            // Add clear formatting instructions for adjustableParameters
            prompt.AppendLine("\nADJUSTABLE PARAMETERS FORMAT:");
            prompt.AppendLine("1. The 'defaultValue' property MUST always be a string, NEVER an array or object");
            prompt.AppendLine("2. For multiple values, concatenate them into a single string with commas");
            prompt.AppendLine("3. Example for categories: \"defaultValue\": \"Electronics, Clothing\" NOT \"defaultValue\": [\"Electronics\", \"Clothing\"]");
            prompt.AppendLine("4. The 'alternatives' property must be an array of strings");
            prompt.AppendLine("5. The 'parameterType' must be one of: \"string\", \"number\", \"date\", \"boolean\", \"category\"");
            prompt.AppendLine("6. Every parameter MUST include the fields: defaultValue, description, alternatives, and parameterType");

            // JSON format instructions
            prompt.AppendLine("\nIMPORTANT FORMAT REQUIREMENTS:");
            prompt.AppendLine("1. The 'alternatives' property inside 'adjustableParameters' MUST be an array of strings, even if there's only one alternative.");
            prompt.AppendLine("2. Use the format: \"alternatives\": [\"option1\", \"option2\"] NOT \"alternatives\": \"Some text\"");
            prompt.AppendLine("3. All arrays should be properly formatted with square brackets, even for single items.");
            prompt.AppendLine("4. The SQL query must be properly escaped as a JSON string.");
            prompt.AppendLine("5. Your entire response must be valid JSON - replace all newlines in SQL with \\n");
            prompt.AppendLine("6. Properly escape all quotes, backslashes and special characters in string values");

            // Example response for unrelated question
            prompt.AppendLine("\nExample response for UNRELATED question:");
            prompt.AppendLine("```json");
            prompt.AppendLine("{");
            prompt.AppendLine("  \"isSchemaRelated\": false,");
            prompt.AppendLine("  \"schemaRelevanceMessage\": \"Your question about weather forecasting doesn't relate to this e-commerce database schema, which contains information about customers, orders, products, and inventory.\",");
            prompt.AppendLine("  \"hasPartiallyUnrelatedContent\": false,");
            prompt.AppendLine("  \"unrelatedQuestionParts\": [],");
            prompt.AppendLine("  \"suggestedTopics\": [\"Customer information\", \"Order details\", \"Product inventory\", \"Sales analytics\", \"Shipping information\"],");
            prompt.AppendLine("  \"suggestedQuestions\": [");
            prompt.AppendLine("    \"Who are our top 10 customers by order value?\",");
            prompt.AppendLine("    \"What products have the lowest inventory levels?\",");
            prompt.AppendLine("    \"How many orders were shipped last month?\"");
            prompt.AppendLine("  ],");
            prompt.AppendLine("  \"sqlQuery\": \"\",");
            prompt.AppendLine("  \"businessExplanation\": \"\",");
            prompt.AppendLine("  \"dbType\": \"ANSI-standard\",");
            prompt.AppendLine("  \"dbNotes\": \"\",");
            prompt.AppendLine("  \"hasAmbiguities\": false,");
            prompt.AppendLine("  \"detectedAmbiguities\": {},");
            prompt.AppendLine("  \"adjustableParameters\": {},");
            prompt.AppendLine("  \"termMapping\": {}");
            prompt.AppendLine("}");
            prompt.AppendLine("```");

            // Example response for partially unrelated question
            prompt.AppendLine("\nExample response for PARTIALLY UNRELATED question:");
            prompt.AppendLine("```json");
            prompt.AppendLine("{");
            prompt.AppendLine("  \"isSchemaRelated\": true,");
            prompt.AppendLine("  \"schemaRelevanceMessage\": \"\",");
            prompt.AppendLine("  \"hasPartiallyUnrelatedContent\": true,");
            prompt.AppendLine("  \"unrelatedQuestionParts\": [\"weather in the shipping destination\"],");
            prompt.AppendLine("  \"suggestedTopics\": [],");
            prompt.AppendLine("  \"suggestedQuestions\": [");
            prompt.AppendLine("    \"What are the most common shipping destinations for our orders?\",");
            prompt.AppendLine("    \"Which shipping methods are used most frequently?\",");
            prompt.AppendLine("    \"What's the average shipping time for each carrier?\"");
            prompt.AppendLine("  ],");
            prompt.AppendLine("  \"sqlQuery\": \"SELECT o.ShippingAddress, COUNT(*) AS OrderCount\\nFROM Orders o\\nGROUP BY o.ShippingAddress\\nORDER BY OrderCount DESC\\nLIMIT 10;\",");
            prompt.AppendLine("  \"businessExplanation\": \"This query shows the top 10 shipping destinations by number of orders. Note that I can't provide information about weather at these destinations as that data isn't in the database.\",");
            prompt.AppendLine("  \"dbType\": \"MySQL\",");
            prompt.AppendLine("  \"dbNotes\": \"This query uses MySQL's LIMIT syntax. For SQL Server, use TOP 10 instead.\",");
            prompt.AppendLine("  \"hasAmbiguities\": false,");
            prompt.AppendLine("  \"detectedAmbiguities\": {},");
            prompt.AppendLine("  \"adjustableParameters\": {");
            prompt.AppendLine("    \"number of destinations\": {");
            prompt.AppendLine("      \"defaultValue\": \"10\",");
            prompt.AppendLine("      \"description\": \"The number of destinations to return\",");
            prompt.AppendLine("      \"alternatives\": [\"5\", \"20\", \"All destinations\"],");
            prompt.AppendLine("      \"parameterType\": \"number\"");
            prompt.AppendLine("    }");
            prompt.AppendLine("  },");
            prompt.AppendLine("  \"termMapping\": {");
            prompt.AppendLine("    \"ShippingAddress\": \"Delivery Location\"");
            prompt.AppendLine("  }");
            prompt.AppendLine("}");
            prompt.AppendLine("```");

            // Example for fully related question
            prompt.AppendLine("\nExample response for FULLY RELATED question:");
            prompt.AppendLine("```json");
            prompt.AppendLine("{");
            prompt.AppendLine("  \"isSchemaRelated\": true,");
            prompt.AppendLine("  \"schemaRelevanceMessage\": \"\",");
            prompt.AppendLine("  \"hasPartiallyUnrelatedContent\": false,");
            prompt.AppendLine("  \"unrelatedQuestionParts\": [],");
            prompt.AppendLine("  \"suggestedTopics\": [],");
            prompt.AppendLine("  \"suggestedQuestions\": [],");
            prompt.AppendLine("  \"sqlQuery\": \"SELECT c.FirstName, c.LastName, SUM(o.TotalAmount) AS TotalSpent\\nFROM Customers c\\nJOIN Orders o ON c.CustomerID = o.CustomerID\\nGROUP BY c.CustomerID, c.FirstName, c.LastName\\nORDER BY TotalSpent DESC\\nLIMIT 10;\",");
            prompt.AppendLine("  \"businessExplanation\": \"This query retrieves the top 10 customers based on their total purchase amounts. It shows each customer's first and last name along with the total amount they've spent across all their orders.\",");
            prompt.AppendLine("  \"dbType\": \"MySQL\",");
            prompt.AppendLine("  \"dbNotes\": \"For SQL Server, replace LIMIT with TOP 10.\",");
            prompt.AppendLine("  \"hasAmbiguities\": true,");
            prompt.AppendLine("  \"detectedAmbiguities\": {");
            prompt.AppendLine("    \"time period\": [");
            prompt.AppendLine("      \"All time\",");
            prompt.AppendLine("      \"Last year\",");
            prompt.AppendLine("      \"Current year\",");
            prompt.AppendLine("      \"Last 6 months\"");
            prompt.AppendLine("    ]");
            prompt.AppendLine("  },");
            prompt.AppendLine("  \"adjustableParameters\": {");
            prompt.AppendLine("    \"number of customers\": {");
            prompt.AppendLine("      \"defaultValue\": \"10\",");
            prompt.AppendLine("      \"description\": \"The number of top customers to display\",");
            prompt.AppendLine("      \"alternatives\": [\"5\", \"20\", \"50\", \"100\"],");
            prompt.AppendLine("      \"parameterType\": \"number\"");
            prompt.AppendLine("    },");
            prompt.AppendLine("    \"product categories\": {");
            prompt.AppendLine("      \"defaultValue\": \"Electronics, Clothing\",");
            prompt.AppendLine("      \"description\": \"The product categories to include\",");
            prompt.AppendLine("      \"alternatives\": [\"Books\", \"Home Goods\", \"Sports Equipment\"],");
            prompt.AppendLine("      \"parameterType\": \"category\"");
            prompt.AppendLine("    }");
            prompt.AppendLine("  },");
            prompt.AppendLine("  \"termMapping\": {");
            prompt.AppendLine("    \"Customers\": \"Client Accounts\",");
            prompt.AppendLine("    \"Orders\": \"Purchases\",");
            prompt.AppendLine("    \"TotalAmount\": \"Revenue\"");
            prompt.AppendLine("  }");
            prompt.AppendLine("}");
            prompt.AppendLine("```");

            return prompt.ToString();
        }

        private string BuildExplanationSystemPrompt(string databaseSchema, Dictionary<string, string> adminDescriptions)
        {
            var prompt = new StringBuilder();

            prompt.AppendLine("You are an AI assistant that helps users understand database queries. " +
                "Your task is to explain natural language questions in terms of how they will be interpreted as database queries. " +
                "Use admin-friendly terminology instead of technical database terms whenever possible.");

            prompt.AppendLine("\nWhen explaining queries:");
            prompt.AppendLine("1. Use natural, conversational language focused on business meaning");
            prompt.AppendLine("2. Explain what data will be retrieved and any filters or conditions");
            prompt.AppendLine("3. Identify any ambiguous terms that could have multiple interpretations");
            prompt.AppendLine("4. Highlight adjustable parameters (dates, thresholds, categories)");
            prompt.AppendLine("5. Use admin-defined descriptions instead of technical database terms");

            prompt.AppendLine("\nFor ambiguities, list each ambiguous term and the possible interpretations.");
            prompt.AppendLine("For adjustable parameters, provide the default value and reasonable alternatives.");

            prompt.AppendLine("\nDatabase schema:");
            prompt.AppendLine(databaseSchema);

            if (adminDescriptions != null && adminDescriptions.Count > 0)
            {
                prompt.AppendLine("\nAdmin descriptions (use these terms instead of technical names):");
                foreach (var description in adminDescriptions)
                {
                    prompt.AppendLine($"- {description.Key}: {description.Value}");
                }
            }

            prompt.AppendLine("\nYour response should be structured as JSON with the following fields:");
            prompt.AppendLine("- explanation: A user-friendly explanation of the query's meaning");
            prompt.AppendLine("- hasAmbiguities: Boolean indicating if any ambiguities were detected");
            prompt.AppendLine("- detectedAmbiguities: Dictionary of ambiguous terms and their possible interpretations");
            prompt.AppendLine("- adjustableParameters: Dictionary of parameters that could be adjusted");
            prompt.AppendLine("- confidenceScore: Number between 0 and 1 indicating confidence in understanding");
            prompt.AppendLine("- previewSql: A preview of the SQL that would be generated (for reference only)");
            prompt.AppendLine("- termMapping: Dictionary mapping technical terms to admin-friendly terms used");

            return prompt.ToString();
        }

        private string BuildSqlGenerationSystemPrompt(string databaseSchema)
        {
            var prompt = new StringBuilder();

            prompt.AppendLine("You are an AI assistant that generates SQL queries from natural language questions. " +
                "Your task is to generate a valid SQL query that correctly answers the given question.");

            prompt.AppendLine("\nYou will be provided with:");
            prompt.AppendLine("1. The original natural language question");
            prompt.AppendLine("2. A confirmed understanding of what the question means");
            prompt.AppendLine("3. Resolved ambiguities (if any)");

            prompt.AppendLine("\nGenerate a SQL query that:");
            prompt.AppendLine("1. Is syntactically correct for SQL Server");
            prompt.AppendLine("2. Uses only table and column names from the provided schema structure");
            prompt.AppendLine("3. If you find complexity in the query, take your time and process it step by step. Accuracy is more important than performance");

            prompt.AppendLine("\nDatabase schema:");
            prompt.AppendLine(databaseSchema);

            prompt.AppendLine("\nReturn ONLY the SQL query without any explanation or formatting.");

            return prompt.ToString();
        }

        /// <summary>
        /// Calls the Databricks API with the given system and user prompts.
        /// </summary>
        /// <param name="systemPrompt">The system prompt to send.</param>
        /// <param name="userPrompt">The user prompt to send.</param>
        /// <returns>The response from the Databricks API.</returns>
        private async Task<string> CallDatabricksApiAsync(string systemPrompt, string userPrompt)
        {
            // Prepare request to the Databricks API
            var requestUrl = $"https://{_databricksHost}/serving-endpoints/{_endpointName}/invocations";

            var messages = new[]
            {
        new { role = "system", content = systemPrompt },
        new { role = "user", content = userPrompt }
    };

            var requestBody = new
            {
                messages = messages,
                model = _modelName,
                max_tokens = 7000,
                temperature = 0.1,
                top_p = 0.95,
                frequency_penalty = 0.0,
                presence_penalty = 0.0
            };

            var content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json");

            // Create a new request message for each call instead of modifying HttpClient directly
            using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            request.Content = content;
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiToken);

            // Send request with a timeout
            var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(_timeoutSeconds));

            var response = await _httpClient.SendAsync(request, cancellationTokenSource.Token);

            // Process response
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Databricks API error ({response.StatusCode}): {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();

            // Parse the response to extract message content
            var responseJson = JsonDocument.Parse(responseContent);

            try
            {
                return responseJson.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();
            }
            catch
            {
                // Fallback if the expected structure is not found
                return responseContent;
            }
        }

        private SqlGenerationWithExplanationResponse ParseSqlExplanationResponse(string response, string originalQuestion)
        {
            try
            {
                var result = new SqlGenerationWithExplanationResponse
                {
                    OriginalQuestion = originalQuestion,
                    Success = true
                };

                // Extract JSON from markdown code block if present
                string jsonContent = response;
                if (response.Trim().StartsWith("```json"))
                {
                    int startIndex = response.IndexOf('{');
                    int endIndex = response.LastIndexOf('}');

                    if (startIndex >= 0 && endIndex >= 0 && endIndex > startIndex)
                    {
                        jsonContent = response.Substring(startIndex, endIndex - startIndex + 1);
                    }
                }

                // Try to parse as JSON
                if (jsonContent.Trim().StartsWith("{") && jsonContent.Trim().EndsWith("}"))
                {
                    try
                    {
                        // Create custom JSON options with proper handling of special characters
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            AllowTrailingCommas = true,
                            ReadCommentHandling = JsonCommentHandling.Skip
                        };

                        // Pre-process the JSON string to ensure escape sequences are properly handled
                        // Convert all literal newlines in SQL query to proper JSON escaped newlines
                        jsonContent = NormalizeJsonEscapeSequences(jsonContent);

                        // Deserialize with the customized options
                        var parsed = JsonSerializer.Deserialize<SqlGenerationWithExplanationResponse>(
                            jsonContent,
                            options);

                        if (parsed != null)
                        {
                            parsed.OriginalQuestion = originalQuestion;
                            parsed.Success = true;
                            return parsed;
                        }
                    }
                    catch (JsonException jsonEx)
                    {
                        // Log the JSON parsing exception with the problematic content
                        Console.Error.WriteLine($"JSON parsing error: {jsonEx.Message}");
                        Console.Error.WriteLine($"JSON content: {jsonContent}");

                        // Fall back to text extraction
                    }
                }

                // Extract SQL from text response
                result.SqlQuery = ExtractSqlFromText(response);

                // Extract business explanation
                string explanation = response;
                if (response.Contains("```sql"))
                {
                    // Try to extract explanation part after SQL code block
                    var parts = response.Split("```", StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2)
                    {
                        explanation = parts[parts.Length - 1].Trim();
                    }
                }

                result.BusinessExplanation = explanation;
                result.IsSchemaRelated = true;

                return result;
            }
            catch (Exception ex)
            {
                return new SqlGenerationWithExplanationResponse
                {
                    Success = false,
                    ErrorMessage = $"Error parsing response: {ex.Message}",
                    OriginalQuestion = originalQuestion
                };
            }
        }

        /// <summary>
        /// Normalizes JSON escape sequences to ensure proper parsing
        /// </summary>
        private string NormalizeJsonEscapeSequences(string json)
        {
            // Common problematic patterns to fix

            // Handle the case of \n\ which is causing the error
            // Replace the pattern \n\ with \n (removing the trailing backslash)
            json = Regex.Replace(json, @"\\n\\(?=[^\\])", "\\n");

            // Handle other potentially problematic escape sequences
            // Replace literal newlines in string values with proper JSON \n
            // json = Regex.Replace(json, @"(?<=([""])(?:[^""\\]|\\[^"])*?)[\r\n] + (?= (?: [^""\\] |\\[^"])*?\1)", "\\n");
            json = System.Text.RegularExpressions.Regex.Replace(json, @"(?<=([[{](?:[^""\\][\\][""])+))\[\r\n] + (?= (?:[^""\\][\\][""])+[\]}])", "\\n");
         

            // Note: This is a simplified approach and may need further refinement
            // based on the specific patterns in your data

            return json;
        }

        private ExplanationResponse ParseExplanationResponse(string response)
        {
            try
            {
                // Try to parse as JSON
                if (response.Trim().StartsWith("{") && response.Trim().EndsWith("}"))
                {
                    try
                    {
                        var parsed = JsonSerializer.Deserialize<ExplanationResponse>(
                            response,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        if (parsed != null)
                            return parsed;
                    }
                    catch
                    {
                        // JSON parsing failed, continue to text response
                    }
                }

                return new ExplanationResponse();
            }
            catch (Exception ex)
            {
                return new ExplanationResponse
                {
                    Explanation = $"Error parsing explanation: {ex.Message}",
                    HasAmbiguities = false,
                    ConfidenceScore = 0.5
                };
            }
        }

        private string ExtractSqlFromText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            // Extract SQL code from markdown code blocks
            if (text.Contains("```sql"))
            {
                int start = text.IndexOf("```sql") + 6;
                int end = text.IndexOf("```", start);
                if (end > start)
                {
                    return text.Substring(start, end - start).Trim();
                }
            }

            if (text.Contains("```"))
            {
                int start = text.IndexOf("```") + 3;
                int end = text.IndexOf("```", start);
                if (end > start)
                {
                    string code = text.Substring(start, end - start).Trim();
                    if (code.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase) ||
                        code.StartsWith("WITH", StringComparison.OrdinalIgnoreCase))
                    {
                        return code;
                    }
                }
            }

            // Look for SQL statements without code blocks
            string[] sqlKeywords = { "SELECT", "WITH" };
            foreach (var keyword in sqlKeywords)
            {
                int keywordIndex = text.IndexOf(keyword, StringComparison.OrdinalIgnoreCase);
                if (keywordIndex >= 0)
                {
                    // Look for end of statement
                    int endIndex = text.IndexOf(";", keywordIndex);
                    if (endIndex > keywordIndex)
                    {
                        return text.Substring(keywordIndex, endIndex - keywordIndex + 1).Trim();
                    }
                    else
                    {
                        // If no semicolon, try to find end of text or next paragraph
                        endIndex = text.IndexOf("\n\n", keywordIndex);
                        if (endIndex > keywordIndex)
                        {
                            return text.Substring(keywordIndex, endIndex - keywordIndex).Trim();
                        }
                        else
                        {
                            // Take the rest of the text
                            return text.Substring(keywordIndex).Trim();
                        }
                    }
                }
            }

            return string.Empty;
        }

        #endregion
    }
}