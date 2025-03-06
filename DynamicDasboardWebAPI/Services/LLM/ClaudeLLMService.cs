using DynamicDashboardCommon.Models;
using DynamicDashboardCommon.Models.LLM;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
    /// Implementation of ILlmService using Anthropic's Claude API
    /// </summary>
    public class ClaudeLLMService : ILLMService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ClaudeLLMService> _logger;
        private readonly string _apiKey;
        private readonly string _model;
        private readonly string _apiEndpoint;

        public ClaudeLLMService(HttpClient httpClient, IConfiguration configuration, ILogger<ClaudeLLMService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Get configuration values
            _apiKey = _configuration["Claude:ApiKey"]
                ?? throw new InvalidOperationException("Claude API key not found in configuration");

            _model = _configuration["Claude:Model"] ?? "claude-3-sonnet-20240229";
            _apiEndpoint = _configuration["Claude:Endpoint"] ?? "https://api.anthropic.com/v1/messages";
        }

        /// <inheritdoc/>
        public async Task<ExplanationResponse> GenerateExplanationAsync(
            string question,
            string databaseSchema,
            Dictionary<string, string> adminDescriptions = null)
        {
            try
            {
                _logger.LogInformation("Generating explanation for question: {Question}", question);

                // Build the system prompt
                var systemPrompt = BuildExplanationSystemPrompt(databaseSchema, adminDescriptions);

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
                _logger.LogError(ex, "Error generating explanation for question: {Question}", question);
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
                _logger.LogInformation("Generating SQL for question: {Question}", question);

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

                // Call Claude API
                var response = await CallClaudeApiAsync(systemPrompt, userPrompt.ToString());

                // Extract SQL from response (assuming the response is just the SQL)
                return response.Trim();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating SQL for question: {Question}", question);
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
                _logger.LogInformation("Generating result explanation for question: {Question}", question);

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
                _logger.LogError(ex, "Error generating result explanation for question: {Question}", question);
                throw new ApplicationException("Failed to generate result explanation", ex);
            }
        }

        #region Private Helper Methods

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
            prompt.AppendLine("2. Uses proper table and column names from the schema");
            prompt.AppendLine("3. Includes appropriate JOINs when needed");
            prompt.AppendLine("4. Applies any filters specified in the question");
            prompt.AppendLine("5. Returns only the requested data");

            prompt.AppendLine("\nDatabase schema:");
            prompt.AppendLine(databaseSchema);

            prompt.AppendLine("\nReturn ONLY the SQL query without any explanation or formatting.");

            return prompt.ToString();
        }

        private async Task<string> CallClaudeApiAsync(string systemPrompt, string userPrompt)
        {
            // Prepare request
            var requestBody = new
            {
                model = _model,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                temperature = 0.2,
                max_tokens = 2000
            };

            var content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json");

            // Set headers
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

            // Send request
            var response = await _httpClient.PostAsync(_apiEndpoint, content);

            // Process response
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Claude API error: {StatusCode} - {Error}", response.StatusCode, errorContent);
                throw new ApplicationException($"Claude API error: {response.StatusCode}");
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

            return messageContent;
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
                _logger.LogError(ex, "Error parsing explanation response: {Response}", jsonResponse);
                return new ExplanationResponse();
                
                // Return a basic response when parsing fails
                //return new ExplanationResponse
                //{
                //    Explanation = jsonResponse,
                //    HasAmbiguities = false,
                //    ConfidenceScore = 0.5 //temp
                //};
            }
        }

        #endregion
    }
}