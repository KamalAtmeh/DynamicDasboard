using DynamicDashboardCommon.Models;
using DynamicDashboardCommon.Models.LLM;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
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
            prompt.AppendLine("- Qualify all column names with table aliases (e.g., users.name)");
            prompt.AppendLine("- Handle NULL values appropriately");
            prompt.AppendLine("- Ensure GROUP BY includes all non-aggregated columns");
            prompt.AppendLine("- Use only SELECT queries (no data modification)");

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

            // Set headers
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiToken);
            _httpClient.Timeout = TimeSpan.FromSeconds(_timeoutSeconds);

            // Send request
            var response = await _httpClient.PostAsync(requestUrl, content);

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

                // Try to parse as JSON first
                if (response.Trim().StartsWith("{") && response.Trim().EndsWith("}"))
                {
                    try
                    {
                        var parsed = JsonSerializer.Deserialize<SqlGenerationWithExplanationResponse>(
                            response,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        if (parsed != null)
                        {
                            parsed.OriginalQuestion = originalQuestion;
                            parsed.Success = true;
                            return parsed;
                        }
                    }
                    catch
                    {
                        // Parsing as JSON failed, continue to text extraction
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

                // Fallback to text response
                return new ExplanationResponse
                {
                    Explanation = response,
                    HasAmbiguities = false,
                    ConfidenceScore = 0.7,
                    DetectedAmbiguities = new Dictionary<string, List<string>>(),
                    AdjustableParameters = new Dictionary<string, QueryParameterOptions>(),
                    PreviewSql = ExtractSqlFromText(response)
                };
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