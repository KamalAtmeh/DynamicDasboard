using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using OfficeOpenXml;
using System.IO;
using System.Threading.Tasks;

namespace DynamicDasboardWebAPI.Services
{
    /// <summary>
    /// Service to generate SQL queries based on provided schema and questions, and process Excel files to insert generated queries.
    /// </summary>
    public class QueryGeneratorService
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryGeneratorService"/> class.
        /// </summary>
        /// <param name="config">The configuration settings.</param>
        /// <param name="httpClient">The HTTP client for making API requests.</param>
        public QueryGeneratorService(IConfiguration config, HttpClient httpClient)
        {
            _config = config;
            _httpClient = httpClient;
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial; // Set EPPlus license context
        }

        /// <summary>
        /// Processes an Excel file to generate SQL queries based on the schema and questions provided in the file.
        /// </summary>
        /// <param name="fileStream">The stream of the Excel file to process.</param>
        /// <returns>A byte array representing the modified Excel file.</returns>
        public async Task<byte[]> ProcessExcelFile(Stream fileStream)
        {
            using (var package = new ExcelPackage(fileStream))
            {
                var worksheet = package.Workbook.Worksheets[0];
                var rowCount = worksheet.Dimension.Rows;

                for (int row = 2; row <= rowCount; row++) // Start from row 2 to skip header
                {
                    var schema = worksheet.Cells[row, 1].Text;
                    var question = worksheet.Cells[row, 2].Text;

                    var result = await GenerateQuery(schema, question);
                    if (result is JsonResult jsonResult && jsonResult.Value != null)
                    {
                        // Extract the query from the JSON response
                        var query = ((dynamic)jsonResult.Value).query;

                        // Ensure the query is clean (remove any additional text)
                        query = query.Trim(); // Remove leading/trailing whitespace

                        // Write only the query to the Excel file
                        worksheet.Cells[row, 5].Value = query; // Fill the 5th column with the SQL query
                    }
                }

                return package.GetAsByteArray();
            }
        }

        /// <summary>
        /// Generates a SQL query based on the provided schema and question.
        /// </summary>
        /// <param name="schema">The database schema.</param>
        /// <param name="question">The question to generate the query for.</param>
        /// <returns>An <see cref="IActionResult"/> containing the generated query or error message.</returns>
        public async Task<IActionResult> GenerateQuery(string schema, string question)
        {
            try
            {
                // Validate the schema
                if (string.IsNullOrWhiteSpace(schema) || !IsSchemaValid(schema))
                {
                    return new JsonResult(new
                    {
                        error = "The provided schema is unclear or invalid. Please provide a proper schema.",
                        suggestions = new[]
                        {
                                "Provide a schema with table and column definitions.",
                                "Ensure the schema includes CREATE TABLE statements.",
                                "Check for missing or incomplete table definitions."
                            }
                    });
                }

                // Generate the SQL query or validate the question
                var apiKey = _config["DeepSeek:ApiKey"];
                var endpoint = _config["DeepSeek:Endpoint"];

                var systemMessage = $@"
    You are a SQL expert. Given the following database schema:
    {schema}

    Rules:
    1. Use proper table joins to retrieve data from multiple tables.
    2. Apply filters and conditions as needed.
    3. Use DISTINCT or GROUP BY to avoid duplicate rows.
    4. Use aggregate functions (e.g., SUM, COUNT, AVG) for calculations.
    5. Ensure the query is syntactically correct and optimized.
    6. Make sure that you are providing only the query in the output without explanations.
    7. If the question is not relevant to the schema, return an error message and suggest 3 alternative questions.
    ";

                var prompt = $"Generate a SQL query for: {question}";

                var temperature = double.TryParse(_config["Model:Model_Temp"], out var temp) ? temp : 0.0;
                var maxTokens = int.TryParse(_config["Model:MaxTokens"], out var tokens) ? tokens : 300;

                var requestBody = new
                {
                    model = "deepseek-chat",
                    messages = new[]
                    {
                            new { role = "system", content = systemMessage },
                            new { role = "user", content = prompt }
                        },
                    temperature = temperature,
                    max_tokens = maxTokens,
                    stream = false
                };

                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

                var response = await _httpClient.PostAsync(
                    endpoint,
                    new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<DeepSeekResponse>(content);

                    var responseText = result?.choices[0].message.content;

                    // Check if the response is an error message or a SQL query
                    if (responseText.StartsWith("Error:"))
                    {
                        // Extract error message and suggestions
                        var errorMessage = responseText.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)[0];
                        var suggestions = responseText.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
                                                     .Skip(1)
                                                     .Take(3)
                                                     .ToArray();

                        return new JsonResult(new
                        {
                            error = errorMessage,
                            suggestions = suggestions
                        });
                    }
                    else
                    {
                        // Return the SQL query
                        return new JsonResult(new { query = responseText });
                    }
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                return new JsonResult(new { error = $"Error: {response.StatusCode} - {errorContent}" });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { error = $"Exception: {ex.Message}" });
            }
        }

        /// <summary>
        /// Validates the provided schema.
        /// </summary>
        /// <param name="schema">The database schema to validate.</param>
        /// <returns><c>true</c> if the schema is valid; otherwise, <c>false</c>.</returns>
        private bool IsSchemaValid(string schema)
        {
            // Basic validation: Check if the schema contains CREATE TABLE statements
            return schema.Contains("CREATE TABLE", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Represents the response from the DeepSeek API.
        /// </summary>
        private class DeepSeekResponse
        {
            public required Choice[] choices { get; set; }
        }

        /// <summary>
        /// Represents a choice in the DeepSeek API response.
        /// </summary>
        private class Choice
        {
            public required Message message { get; set; }
        }

        /// <summary>
        /// Represents a message in the DeepSeek API response.
        /// </summary>
        private class Message
        {
            public required string content { get; set; }
        }
    }
}