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
    /// Implementation of ILLMService using SQLCoder 7b-2 model via Friendli.ai API
    /// </summary>
    public class SQLCoderLLMService : ILLMService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly string _apiKey;
        private readonly string _model;
        private readonly string _apiEndpoint;
        private readonly int _timeoutSeconds;

        public SQLCoderLLMService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            // Get configuration values
            _apiKey = _configuration["SQLCoder:ApiKey"]
                ?? throw new InvalidOperationException("SQLCoder API key not found in configuration");

            _model = _configuration["SQLCoder:Model"] ?? "sqlcoder-7b-2";
            _apiEndpoint = _configuration["SQLCoder:Endpoint"] ?? "https://api.friendli.ai/dedicated/v1/completions";
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
                // Build prompts
                var systemPrompt = BuildSQLGenerationSystemPrompt(databaseSchema, dbDescriptions);
                var userPrompt = $"Question: {question}\n\nPlease generate a SQL query for this question and explain what it does.";

                // Call API
                var response = await CallSQLCoderApiAsync(systemPrompt, userPrompt);

                // Parse response into structured format
                return ParseSqlResponse(response, question);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error generating SQL with explanation: {ex.Message}", ex);
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
                // Build prompt for explanation
                var systemPrompt = BuildExplanationSystemPrompt(databaseSchema, adminDescriptions);
                var userPrompt = $"Question: {question}\n\nPlease explain how you would interpret this question without generating the SQL yet.";

                // Call API
                var response = await CallSQLCoderApiAsync(systemPrompt, userPrompt);

                // Parse response
                return ParseExplanationResponse(response);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error generating explanation: {ex.Message}", ex);
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
                // Build prompts with confirmed understanding
                var systemPrompt = BuildSQLGenerationSystemPrompt(databaseSchema, null);

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

                userPrompt.AppendLine("\nGenerate only the SQL query to answer this question without any explanation.");

                // Call API
                var response = await CallSQLCoderApiAsync(systemPrompt, userPrompt.ToString());

                // Extract SQL query from response
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
                // Build prompts
                var systemPrompt = "You are a helpful assistant explaining database query results. " +
                    "Provide clear, concise explanations focused on insights from the data.";

                var userPrompt = new StringBuilder();
                userPrompt.AppendLine($"Original question: {question}");
                userPrompt.AppendLine($"SQL query used: {sql}");
                userPrompt.AppendLine("\nQuery results (first few rows):");

                // Add sample of results
                var resultSample = results.Count <= 5 ? results : results.GetRange(0, 5);
                userPrompt.AppendLine(JsonSerializer.Serialize(resultSample, new JsonSerializerOptions { WriteIndented = true }));
                userPrompt.AppendLine($"\nTotal rows returned: {results.Count}");
                userPrompt.AppendLine("\nPlease provide a brief, user-friendly explanation of these results in 2-3 sentences.");

                // Call API
                var response = await CallSQLCoderApiAsync(systemPrompt, userPrompt.ToString());

                return response.Trim();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error generating result explanation: {ex.Message}", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<string> GenerateSchemaAnalysisAsync(string prompt)
        {
            try
            {
                var systemPrompt = "You are an expert database analyst helping improve database schema usability.";
                var response = await CallSQLCoderApiAsync(systemPrompt, prompt);
                return response;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error generating schema analysis: {ex.Message}", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<string> GenerateTermSuggestionsAsync(string prompt)
        {
            try
            {
                var systemPrompt = "You are an expert in database terminology helping map technical terms to business terms.";
                var response = await CallSQLCoderApiAsync(systemPrompt, prompt);
                return response;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error generating term suggestions: {ex.Message}", ex);
            }
        }

        public async Task<string> GenerateChartExplanationAsync(string question, string databaseSchema)
        {
            try
            {
                var systemPrompt = $@"You are a business intelligence expert analyzing dashboard chart requests.

Database Schema:
{databaseSchema}

Your task is to provide a clear, concise explanation of what data will be visualized based on the user's request.

Focus on:
- What data will be shown
- What filters or conditions will be applied
- The business value or insights this visualization provides

Keep the explanation to 2-3 sentences maximum.
Use business-friendly language, not technical database jargon.
Do NOT generate SQL - only explain what the chart will show.";

                var userPrompt = $"Chart Request: {question}\n\nProvide a clear explanation of what this chart will visualize:";

                // Call API
                var response = await CallSQLCoderApiAsync(systemPrompt, userPrompt);

                // Return the explanation directly (no JSON parsing needed)
                return response.Trim();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error generating chart explanation: {ex.Message}", ex);
            }
        }

        #region Private Helper Methods

        private string BuildSQLGenerationSystemPrompt(string databaseSchema, Dictionary<string, string> dbDescriptions = null)
        {
            var prompt = new StringBuilder();

            prompt.AppendLine("You are an expert SQL developer that generates accurate SQL queries from natural language questions. " +
                "Your specialty is understanding database schemas and writing efficient, correct SQL.");

            prompt.AppendLine("\nWhen generating SQL:");
            prompt.AppendLine("1. Use only tables and columns specified in the schema");
            prompt.AppendLine("2. Choose appropriate JOINs based on the relationships");
            prompt.AppendLine("3. Apply proper filtering conditions");
            prompt.AppendLine("4. Format the SQL with proper indentation for readability");
            prompt.AppendLine("5. Qualify column names with table names or aliases");
            prompt.AppendLine("6. Include a business-friendly explanation of what the query does");

            prompt.AppendLine("\nDatabase Schema:");
            prompt.AppendLine(databaseSchema);

            if (dbDescriptions != null && dbDescriptions.Count > 0)
            {
                prompt.AppendLine("\nBusiness Terminology (use these terms in your explanations):");
                foreach (var desc in dbDescriptions)
                {
                    prompt.AppendLine($"- {desc.Key}: {desc.Value}");
                }
            }

            return prompt.ToString();
        }

        private string BuildExplanationSystemPrompt(string databaseSchema, Dictionary<string, string> adminDescriptions = null)
        {
            var prompt = new StringBuilder();

            prompt.AppendLine("You are an AI assistant that helps users understand database queries. " +
                "Your task is to explain natural language questions in terms of how they would be interpreted as database queries.");

            prompt.AppendLine("\nWhen explaining:");
            prompt.AppendLine("1. Use natural, conversational language focused on business meaning");
            prompt.AppendLine("2. Explain what data would be retrieved and any filters or conditions");
            prompt.AppendLine("3. Identify any ambiguous terms that could have multiple interpretations");
            prompt.AppendLine("4. Highlight adjustable parameters (dates, thresholds, categories)");
            prompt.AppendLine("5. Use admin-friendly terminology instead of technical database terms");

            prompt.AppendLine("\nDatabase Schema:");
            prompt.AppendLine(databaseSchema);

            if (adminDescriptions != null && adminDescriptions.Count > 0)
            {
                prompt.AppendLine("\nAdmin Descriptions (use these terms instead of technical names):");
                foreach (var desc in adminDescriptions)
                {
                    prompt.AppendLine($"- {desc.Key}: {desc.Value}");
                }
            }

            prompt.AppendLine("\nPlease provide your response as JSON with:");
            prompt.AppendLine("- explanation: A user-friendly explanation of what the query would retrieve");
            prompt.AppendLine("- hasAmbiguities: Boolean indicating if ambiguities were detected");
            prompt.AppendLine("- detectedAmbiguities: Dictionary of ambiguous terms and possible interpretations");
            prompt.AppendLine("- adjustableParameters: Dictionary of parameters that could be adjusted");
            prompt.AppendLine("- confidenceScore: Number between 0 and 1 indicating confidence");
            prompt.AppendLine("- previewSql: A preview of the SQL that would be generated");

            return prompt.ToString();
        }

        private async Task<string> CallSQLCoderApiAsync(string systemPrompt, string userPrompt)
        {
            try
            {
                // Prepare API request
                var messages = new List<object>
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                };

                var requestData = new
                {
                    messages = messages,
                    model = _model,
                    temperature = 0.1, // Low temperature for more deterministic SQL generation
                    max_tokens = 2000
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(requestData),
                    Encoding.UTF8,
                    "application/json");

                // Set request headers
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
                _httpClient.Timeout = TimeSpan.FromSeconds(_timeoutSeconds);

                // Send request to API
                var response = await _httpClient.PostAsync(_apiEndpoint, content);

                // Process response
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API request failed with status {response.StatusCode}: {errorContent}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var jsonResponse = JsonDocument.Parse(responseContent);

                // Extract content from response (adjust this based on actual response format)
                var messageContent = jsonResponse.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                return messageContent;
            }
            catch (Exception ex)
            {
                throw new Exception($"API call failed: {ex.Message}", ex);
            }
        }

        private SqlGenerationWithExplanationResponse ParseSqlResponse(string response, string originalQuestion)
        {
            try
            {
                // First try to parse as JSON response
                if (response.Trim().StartsWith("{") && response.Trim().EndsWith("}"))
                {
                    try
                    {
                        var jsonResponse = JsonSerializer.Deserialize<SqlGenerationWithExplanationResponse>(response,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        if (jsonResponse != null)
                        {
                            jsonResponse.OriginalQuestion = originalQuestion;
                            jsonResponse.Success = true;
                            return jsonResponse;
                        }
                    }
                    catch
                    {
                        // JSON parsing failed, continue to text parsing
                    }
                }

                // If JSON parsing failed, attempt to extract SQL and explanation from text
                string sql = ExtractSqlFromText(response);
                string explanation = ExtractExplanationFromText(response);

                return new SqlGenerationWithExplanationResponse
                {
                    OriginalQuestion = originalQuestion,
                    SqlQuery = sql,
                    BusinessExplanation = explanation,
                    IsSchemaRelated = true,
                    DbType = "SQL",
                    Success = !string.IsNullOrEmpty(sql)
                };
            }
            catch (Exception ex)
            {
                // Return a basic response with the error
                return new SqlGenerationWithExplanationResponse
                {
                    OriginalQuestion = originalQuestion,
                    Success = false,
                    ErrorMessage = $"Failed to parse response: {ex.Message}",
                    BusinessExplanation = response
                };
            }
        }

        private ExplanationResponse ParseExplanationResponse(string response)
        {
            try
            {
                // Try to parse as JSON first
                if (response.Trim().StartsWith("{") && response.Trim().EndsWith("}"))
                {
                    try
                    {
                        var jsonResponse = JsonSerializer.Deserialize<ExplanationResponse>(response,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        if (jsonResponse != null)
                            return jsonResponse;
                    }
                    catch
                    {
                        // JSON parsing failed, fall back to simple explanation
                    }
                }

                // Basic fallback if JSON parsing fails
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
                // Return minimal response to avoid breaking the application
                return new ExplanationResponse
                {
                    Explanation = $"Error parsing explanation: {ex.Message}. Raw response: {response}",
                    HasAmbiguities = false,
                    ConfidenceScore = 0.5
                };
            }
        }

        private string ExtractSqlFromText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            // Check for SQL code blocks (```sql...```)
            int sqlBlockStart = text.IndexOf("```sql");
            if (sqlBlockStart >= 0)
            {
                sqlBlockStart += 6; // Move past ```sql
                int sqlBlockEnd = text.IndexOf("```", sqlBlockStart);
                if (sqlBlockEnd > sqlBlockStart)
                {
                    return text.Substring(sqlBlockStart, sqlBlockEnd - sqlBlockStart).Trim();
                }
            }

            // Check for generic code blocks (```...```)
            int codeBlockStart = text.IndexOf("```");
            if (codeBlockStart >= 0)
            {
                codeBlockStart += 3; // Move past ```
                int codeBlockEnd = text.IndexOf("```", codeBlockStart);
                if (codeBlockEnd > codeBlockStart)
                {
                    string codeBlock = text.Substring(codeBlockStart, codeBlockEnd - codeBlockStart).Trim();
                    if (codeBlock.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase) ||
                        codeBlock.StartsWith("WITH", StringComparison.OrdinalIgnoreCase))
                    {
                        return codeBlock;
                    }
                }
            }

            // Look for SQL keywords
            string[] lines = text.Split('\n');
            StringBuilder sql = new StringBuilder();
            bool inSql = false;

            foreach (string line in lines)
            {
                if (!inSql && (line.Trim().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase) ||
                               line.Trim().StartsWith("WITH", StringComparison.OrdinalIgnoreCase)))
                {
                    inSql = true;
                    sql.AppendLine(line);
                }
                else if (inSql)
                {
                    if (line.Contains("```") || line.Contains("Explanation:") ||
                        (string.IsNullOrWhiteSpace(line) && sql.Length > 0 && !line.Contains("FROM")))
                    {
                        break;
                    }
                    sql.AppendLine(line);
                }
            }

            return sql.ToString().Trim();
        }

        private string ExtractExplanationFromText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            // Look for explanation section
            string[] markers = new[] { "Explanation:", "This query:", "The SQL query:", "This SQL:" };

            foreach (string marker in markers)
            {
                int explanationStart = text.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
                if (explanationStart >= 0)
                {
                    explanationStart += marker.Length;

                    // Look for the end of the explanation (next section or end of text)
                    int explanationEnd = text.Length;
                    string[] endMarkers = new[] { "\n\n```", "\n\n#", "\n\nNote:" };

                    foreach (string endMarker in endMarkers)
                    {
                        int endPos = text.IndexOf(endMarker, explanationStart, StringComparison.OrdinalIgnoreCase);
                        if (endPos >= 0 && endPos < explanationEnd)
                        {
                            explanationEnd = endPos;
                        }
                    }

                    return text.Substring(explanationStart, explanationEnd - explanationStart).Trim();
                }
            }

            // If no explanation section found, return the text after SQL or a placeholder
            int sqlEnd = text.LastIndexOf("```");
            if (sqlEnd >= 0 && sqlEnd + 3 < text.Length)
            {
                return text.Substring(sqlEnd + 3).Trim();
            }

            return "No explicit explanation provided.";
        }

        #endregion
    }
}