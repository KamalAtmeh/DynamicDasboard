
using DynamicDashboardCommon.Models;
using DynamicDashboardCommon.Models.LLM;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySqlX.XDevAPI;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace DynamicDasboardWebAPI.Services.LLM
{
    /// <summary>
    /// Implementation of ILlmService using Anthropic's Claude API
    /// </summary>
    public class ClaudeLLMService : ILLMService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly string _apiKey;
        private readonly string _model;
        private readonly string _apiEndpoint;
        private readonly int timeOutSeconds;
        //temp //todo move common methods into a common class and keep only to what is related to the LLM type here Claude for example
        public ClaudeLLMService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));


            // Get configuration values
            _apiKey = _configuration["Claude:ApiKey"]
                ?? throw new InvalidOperationException("Claude API key not found in configuration");

            _model = _configuration["Claude:Model"] ?? "claude-3-sonnet-20240229";
            _apiEndpoint = _configuration["Claude:Endpoint"] ?? "https://api.anthropic.com/v1/messages";
            timeOutSeconds = _configuration.GetValue<int>("LlmService: Timeout", 150);
        }

        // Add these methods to the ClaudeLLMService class

        /// <inheritdoc/>
        public async Task<SqlExplanationResponse> GenerateSqlWithExplanationAsync(
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

                // Call Claude API
                var response = await CallClaudeApiAsync(systemPrompt, userPrompt);

                // Parse the SQL explanation response
                return ParseSqlExplanationResponse(response);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private SqlExplanationResponse ParseSqlExplanationResponse(string jsonResponse)
        {
            try
            {
                // Extract JSON from the response
                var jsonStart = jsonResponse.IndexOf('{');
                var jsonEnd = jsonResponse.LastIndexOf('}');

                if (jsonStart >= 0 && jsonEnd > jsonStart)
                {
                    var json = jsonResponse.Substring(jsonStart, jsonEnd - jsonStart + 1);
                    var response = System.Text.Json.JsonSerializer.Deserialize<SqlExplanationResponse>(json,
                        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (response != null)
                        return response;
                }

                // If we couldn't parse as JSON or the response is null, create a simple response
                return new SqlExplanationResponse
                {
                    SqlQuery = ExtractSqlFromText(jsonResponse),
                    BusinessExplanation = jsonResponse,
                    //DbType = 
                    HasAmbiguities = false
                };
            }
            catch (Exception ex)
            {
                // Log error
                throw;
            }
        }

        private string ExtractSqlFromText(string text)
        {
            try
            {
                // Try to extract SQL from text by looking for SQL keywords
                var sqlKeywords = new[] { "SELECT", "FROM", "WHERE", "GROUP BY", "ORDER BY", "HAVING", "JOIN" };

                foreach (var keyword in sqlKeywords)
                {
                    var keywordIndex = text.IndexOf(keyword);
                    if (keywordIndex >= 0)
                    {
                        // Find the end of the SQL query (next code block or end of text)
                        var endIndex = text.IndexOf("```", keywordIndex);
                        if (endIndex >= 0)
                            return text.Substring(keywordIndex, endIndex - keywordIndex).Trim();

                        // If no code block end, try to find a natural break
                        endIndex = text.IndexOf("\n\n", keywordIndex);
                        if (endIndex >= 0)
                            return text.Substring(keywordIndex, endIndex - keywordIndex).Trim();

                        // If no natural break, return the rest of the text
                        return text.Substring(keywordIndex).Trim();
                    }
                }

                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private string BuildSQLScriptwithExplanationSystemPrompt(string databaseSchema, Dictionary<string, string> adminDescriptions)
        {
            try
            {
                var prompt = new StringBuilder();
                prompt.AppendLine("You are an AI assistant that generates SQL queries from natural language questions and explains them in business terms. " +
                                "Your primary task is to FIRST create a SQL query based on the user's question and schema, THEN explain what it does using friendly business terminology.");

                // Primary approach and priorities
                prompt.AppendLine("\nFollow this approach:");
                prompt.AppendLine("1. Generate a SQL query that works for the specified database type (or ANSI-standard SQL if no type specified)");
                prompt.AppendLine("2. Explain the query's business meaning using friendly terminology");
                prompt.AppendLine("3. Identify adjustable parameters and potential ambiguities");

                // SQL generation requirements
                prompt.AppendLine("\nSQL Generation Rules:");
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

                // Response format
                prompt.AppendLine("\nYour response should be structured as JSON with the following fields:");
                prompt.AppendLine("- sqlQuery: The generated SQL query that answers the user's question");
                prompt.AppendLine("- businessExplanation: A user-friendly explanation of what the query does");
                prompt.AppendLine("- dbType: The database type this SQL is optimized for (e.g., 'SQL Server', 'MySQL', 'Oracle', 'ANSI-standard')");
                prompt.AppendLine("- dbNotes: Notes about database compatibility or adaptation requirements");
                prompt.AppendLine("- hasAmbiguities: Boolean indicating if any ambiguities were detected");
                prompt.AppendLine("- detectedAmbiguities: Dictionary of ambiguous terms and their possible interpretations");
                prompt.AppendLine("- adjustableParameters: Dictionary of parameters that could be adjusted");
                prompt.AppendLine("- termMapping: Dictionary mapping technical terms to friendly terms (from descriptions) used");

                // JSON format instructions
                prompt.AppendLine("\nIMPORTANT FORMAT REQUIREMENTS:");
                prompt.AppendLine("1. The 'alternatives' property inside 'adjustableParameters' MUST be an array of strings, even if there's only one alternative.");
                prompt.AppendLine("2. Use the format: \"alternatives\": [\"option1\", \"option2\"] NOT \"alternatives\": \"Some text\"");
                prompt.AppendLine("3. All arrays should be properly formatted with square brackets, even for single items.");
                prompt.AppendLine("4. The SQL query must be properly escaped as a JSON string.");

                // Example with database-specific SQL
                prompt.AppendLine("\nHere's a complete example of the expected JSON format, in this example it was SQL Server DB Type:");
                prompt.AppendLine("```json");
                prompt.AppendLine("{");
                prompt.AppendLine("  \"sqlQuery\": \"SELECT TOP 10\\n  u.name AS customer_name,\\n  u.email AS customer_email,\\n  SUM(o.amount) AS total_spent\\nFROM users u\\nINNER JOIN orders o \\n  ON u.id = o.user_id\\nWHERE \\n  o.order_date >= '2023-08-01' \\n  AND o.order_date < '2023-09-01'\\nGROUP BY \\n  u.id, \\n  u.name, \\n  u.email\\nORDER BY total_spent DESC;\",");
                prompt.AppendLine("  \"businessExplanation\": \"This query shows you the top 10 customers who spent the most money during August 2023. It includes each customer's name, email address, and their total spending for that month. The results are ordered from highest spender to lowest.\",");
                prompt.AppendLine("  \"dbType\": \"SQL Server\",");
                prompt.AppendLine("  \"dbNotes\": \"This query uses SQL Server's TOP syntax for row limiting. For other databases, use: MySQL/PostgreSQL/SQLite: LIMIT 10; Oracle 12c+: FETCH FIRST 10 ROWS ONLY; Oracle before 12c: WHERE ROWNUM <= 10.\",");
                prompt.AppendLine("  \"hasAmbiguities\": true,");
                prompt.AppendLine("  \"detectedAmbiguities\": {");
                prompt.AppendLine("    \"customer spending\": [");
                prompt.AppendLine("      \"Total amount spent on all orders\",");
                prompt.AppendLine("      \"Revenue before discounts or returns\"");
                prompt.AppendLine("    ]");
                prompt.AppendLine("  },");
                prompt.AppendLine("  \"adjustableParameters\": {");
                prompt.AppendLine("    \"date range\": {");
                prompt.AppendLine("      \"default\": \"August 2023 (2023-08-01 to 2023-08-31)\",");
                prompt.AppendLine("      \"alternatives\": [\"Last 30 days\", \"Current month\", \"Year to date\", \"Q3 2023\"]");
                prompt.AppendLine("    },");
                prompt.AppendLine("    \"number of customers\": {");
                prompt.AppendLine("      \"default\": 10,");
                prompt.AppendLine("      \"alternatives\": [\"5\", \"20\", \"50\", \"All customers\"]");
                prompt.AppendLine("    }");
                prompt.AppendLine("  },");
                prompt.AppendLine("  \"termMapping\": {");
                prompt.AppendLine("    \"users\": \"Customers\",");
                prompt.AppendLine("    \"orders\": \"Purchases\",");
                prompt.AppendLine("    \"amount\": \"Transaction value\"");
                prompt.AppendLine("  }");
                prompt.AppendLine("}");
                prompt.AppendLine("```");
                prompt.AppendLine("\nIf a specific database type is provided in the user's question or context, ensure the generated SQL is fully compatible with that database system. If no database type is specified, default to ANSI-standard SQL.");

                return prompt.ToString();
            }
            catch (Exception ex)
            {
                throw;
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
                var systemPrompt = BuildSQLScriptwithExplanationSystemPrompt(databaseSchema, adminDescriptions);

                // Build the user prompt
                var userPrompt = $"Question: {question}\n\nPlease explain how you understand this question in user-friendly terms, " +
                    "identify any ambiguities, and list any adjustable parameters.";

                // Call Claude API
                var response = await CallClaudeApiAsync(systemPrompt, userPrompt);

                // Parse the explanation response
                return ParseExplanationResponse(response);
            }
            catch (Exception ex)
            {
                throw;
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

                // Add null check and safe enumeration for resolvedAmbiguities
                if (resolvedAmbiguities != null && resolvedAmbiguities.Count > 0)
                {
                    userPrompt.AppendLine("\nResolved ambiguities:");

                    // Use a safer approach to iterate through dictionary entries
                    foreach (var entry in resolvedAmbiguities)
                    {
                        if (entry.Key != null && entry.Value != null)
                        {
                            userPrompt.AppendLine($"- {entry.Key}: {entry.Value}");
                        }
                    }
                }

                userPrompt.AppendLine("\nGenerate a SQL query that answers this question. Return ONLY the SQL with no explanations.");

                // Call Claude API
                var response = await CallClaudeApiAsync(systemPrompt, userPrompt.ToString());

                // Extract SQL from response (assuming the response is just the SQL)
                return response.Trim();
            }
            catch (Exception ex)
            {
                throw;
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

                // Call Claude API
                var response = await CallClaudeApiAsync(systemPrompt, userPrompt.ToString());

                // Return the explanation
                return response.Trim();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        #region Private Helper Methods

        private string BuildSQLScriptwithExplanationSystemPrompt_Old(string databaseSchema, Dictionary<string, string> adminDescriptions)
        {
            try
            {
                var prompt = new StringBuilder();
                prompt.AppendLine("You are an AI assistant that helps users understand database queries. " +
                        "Your task is to explain natural language questions in terms of how they will be interpreted as database queries. " +
                        "Use friendly terminology from provided descriptions instead of technical database terms whenever possible.");
                prompt.AppendLine("\nWhen explaining queries:");
                prompt.AppendLine("1. Use natural, conversational language focused on business meaning");
                prompt.AppendLine("2. Explain what data will be retrieved and any filters or conditions");
                prompt.AppendLine("3. Identify any ambiguous terms that could have multiple interpretations");
                prompt.AppendLine("4. Highlight adjustable parameters (dates, thresholds, categories)");
                prompt.AppendLine("5. Use defined descriptions instead of technical database terms");
                prompt.AppendLine("\nFor ambiguities, list each ambiguous term and the possible interpretations.");
                prompt.AppendLine("For adjustable parameters, provide the default value and reasonable alternatives.");
                prompt.AppendLine("\nDatabase schema:");
                prompt.AppendLine(databaseSchema);
                if (adminDescriptions != null && adminDescriptions.Count > 0)
                {
                    prompt.AppendLine("\ndescriptions (use these terms instead of technical names):");
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
                prompt.AppendLine("- termMapping: Dictionary mapping technical terms to friendly terms (from descriptions) used");

                // Add specific instructions about JSON format and arrays
                prompt.AppendLine("\nIMPORTANT FORMAT REQUIREMENTS:");
                prompt.AppendLine("1. The 'alternatives' property inside 'adjustableParameters' MUST be an array of strings, even if there's only one alternative.");
                prompt.AppendLine("2. Use the format: \"alternatives\": [\"option1\", \"option2\"] NOT \"alternatives\": \"Some text\"");
                prompt.AppendLine("3. All arrays should be properly formatted with square brackets, even for single items.");

                // Add a complete example
                prompt.AppendLine("\nHere's a complete example of the expected JSON format:");
                prompt.AppendLine("```json");
                prompt.AppendLine("{");
                prompt.AppendLine("  \"explanation\": \"This query will show the top 10 customers who have spent the most money on orders.\",");
                prompt.AppendLine("  \"hasAmbiguities\": true,");
                prompt.AppendLine("  \"detectedAmbiguities\": {");
                prompt.AppendLine("    \"top customers\": [");
                prompt.AppendLine("      \"Customers with highest total spending\",");
                prompt.AppendLine("      \"Customers with most frequent orders\"");
                prompt.AppendLine("    ],");
                prompt.AppendLine("    \"time period\": [");
                prompt.AppendLine("      \"All time\",");
                prompt.AppendLine("      \"Current year\",");
                prompt.AppendLine("      \"Last 12 months\"");
                prompt.AppendLine("    ]");
                prompt.AppendLine("  },");
                prompt.AppendLine("  \"adjustableParameters\": {");
                prompt.AppendLine("    \"number of customers\": {");
                prompt.AppendLine("      \"default\": 10,");
                prompt.AppendLine("      \"alternatives\": [\"5\", \"20\", \"50\", \"100\"]");
                prompt.AppendLine("    },");
                prompt.AppendLine("    \"sort order\": {");
                prompt.AppendLine("      \"default\": \"Descending (highest first)\",");
                prompt.AppendLine("      \"alternatives\": [\"Ascending (lowest first)\"]");
                prompt.AppendLine("    }");
                prompt.AppendLine("  },");
                prompt.AppendLine("  \"confidenceScore\": 0.9,");
                prompt.AppendLine("  \"previewSql\": \"SELECT c.FirstName + ' ' + c.LastName AS CustomerName, SUM(o.TotalAmount) AS TotalSpent FROM Customers c JOIN Orders o ON c.CustomerID = o.CustomerID GROUP BY c.CustomerID, c.FirstName, c.LastName ORDER BY TotalSpent DESC LIMIT 10;\",");
                prompt.AppendLine("  \"termMapping\": {");
                prompt.AppendLine("    \"Customers\": \"Client accounts\",");
                prompt.AppendLine("    \"Orders\": \"Purchase transactions\",");
                prompt.AppendLine("    \"Total\": \"Purchase amount\"");
                prompt.AppendLine("  }");
                prompt.AppendLine("}");
                prompt.AppendLine("```");

                return prompt.ToString();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private string BuildSqlGenerationSystemPrompt(string databaseSchema)
        {
            try
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
                prompt.AppendLine("2. it is important to make sure Usage of only table and column names from the provided schema structure");
                prompt.AppendLine("3. If you found complexity in the query , take your time and take it step by step . accuracy is more important than performance");
                //prompt.AppendLine("3. Includes appropriate JOINs when needed");
                //prompt.AppendLine("4. Applies any filters specified in the question");
                prompt.AppendLine("3. Returns only the requested data");

                prompt.AppendLine("\nDatabase schema:");
                prompt.AppendLine(databaseSchema);

                prompt.AppendLine("\nReturn ONLY the SQL query without any explanation or formatting.");

                return prompt.ToString();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private async Task<string> CallClaudeApiAsync(string systemPrompt, string userPrompt)
        {
            try
            {
                var objSystemPrompt = new List<object>
                {

                    new
                    {
                        type = "text",
                        text = systemPrompt,
                        cache_control = new { type = "ephemeral" }
                    }
                };
                // Prepare request
                // Prepare request
                var requestBody = new
                {
                    model = _model,
                    system = objSystemPrompt,
                    //system = systemPrompt, // System prompt as a top-level parameter
                    messages = new[]
                    {
                new { role = "user", content = userPrompt
}
                },
                    temperature = 1,
                    max_tokens = 2000
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(requestBody),
                    Encoding.UTF8,
                    "application/json");

                //var reqcontent = "{""model"": "'claude-3-opus-20240229'", ""max_tokens"": 1024, ""messages"": [ {""role"": ""user", "content"": ""Hello, world""}";

                if (!_httpClient.DefaultRequestHeaders.Contains("x-api-key"))
                {
                    _httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);
                }
                // Set headers
                if (!_httpClient.DefaultRequestHeaders.Contains("anthropic-version"))
                {
                    _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
                }
                // _httpClient.DefaultRequestHeaders.Add("Content-Type", "application/json");    
                if (_httpClient.Timeout == TimeSpan.Zero)
                {
                    _httpClient.Timeout = TimeSpan.FromSeconds(timeOutSeconds);
                }



                // Send request
                var response = await _httpClient.PostAsync(_apiEndpoint, content);

                // Process response
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                }



                var responseContent = await response.Content.ReadAsStringAsync();
                var jsonResponse = JsonDocument.Parse(responseContent);

                // Extract content from Claude response
                var messageContent = jsonResponse.RootElement
                    .GetProperty("content")
                    .EnumerateArray()
                    .First()
                    .GetProperty("text")
                    .GetString();

                var usage = jsonResponse.RootElement
                    .GetProperty("usage");

                //var state = result.Usage.CacheCreationInputTokens;
                //                result.Usage.CacheReadInputTokens,
                //                result.Usage.InputTokens);

                return messageContent;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private ExplanationResponse ParseExplanationResponse(string jsonResponse)
        {
            try
            {
                // If the response is already in JSON format, parse it directly
                if (jsonResponse.Trim().StartsWith("{") && jsonResponse.Trim().EndsWith("}"))
                {
                    return JsonSerializer.Deserialize<ExplanationResponse>(jsonResponse,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }

                // Otherwise, try to extract JSON from the response text (in case Claude added extra text)
                var jsonStart = jsonResponse.IndexOf('{');
                var jsonEnd = jsonResponse.LastIndexOf('}');

                if (jsonStart >= 0 && jsonEnd > jsonStart)
                {
                    var json = jsonResponse.Substring(jsonStart, jsonEnd - jsonStart + 1);
                    return JsonSerializer.Deserialize<ExplanationResponse>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }

                // If we couldn't parse as JSON, create a simple explanation response
                //return new ExplanationResponse
                //{
                //    Explanation = jsonResponse,
                //    HasAmbiguities = false,
                //    ConfidenceScore = 0.7 //temp
                //};
                return new ExplanationResponse();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        #endregion
    }
}