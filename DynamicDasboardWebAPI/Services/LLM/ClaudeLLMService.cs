
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

        private SqlGenerationWithExplanationResponse ParseSqlExplanationResponse(string jsonResponse)
        {
            try
            {
                // Extract JSON from the response
                var jsonStart = jsonResponse.IndexOf('{');
                var jsonEnd = jsonResponse.LastIndexOf('}');

                if (jsonStart >= 0 && jsonEnd > jsonStart)
                {
                    var json = jsonResponse.Substring(jsonStart, jsonEnd - jsonStart + 1);
                    var response = System.Text.Json.JsonSerializer.Deserialize<SqlGenerationWithExplanationResponse>(json,
                        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (response != null)
                        return response;
                }

                // If we couldn't parse as JSON or the response is null, create a simple response
                return new SqlGenerationWithExplanationResponse
                {
                    GeneratedSql = ExtractSqlFromText(jsonResponse),
                    BusinessExplanation = jsonResponse,
                    IsSchemaRelated = false,
                    SchemaRelevanceMessage = "Unable to parse the response properly. The question may not be related to the database schema.",
                    SuggestedTopics = new List<string> { "Customer data", "Orders", "Products", "Inventory" },
                    SuggestedQuestions = new List<string> {
                "Show me all customers",
                "What are the top selling products?",
                "How many orders were placed last month?"
            },
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

                // JSON format instructions
                prompt.AppendLine("\nIMPORTANT FORMAT REQUIREMENTS:");
                prompt.AppendLine("1. The 'alternatives' property inside 'adjustableParameters' MUST be an array of strings, even if there's only one alternative.");
                prompt.AppendLine("2. Use the format: \"alternatives\": [\"option1\", \"option2\"] NOT \"alternatives\": \"Some text\"");
                prompt.AppendLine("3. All arrays should be properly formatted with square brackets, even for single items.");
                prompt.AppendLine("4. The SQL query must be properly escaped as a JSON string.");

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
                prompt.AppendLine("      \"default\": 10,");
                prompt.AppendLine("      \"alternatives\": [\"5\", \"20\", \"All destinations\"]");
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
                prompt.AppendLine("      \"default\": 10,");
                prompt.AppendLine("      \"alternatives\": [\"5\", \"20\", \"50\", \"100\"]");
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