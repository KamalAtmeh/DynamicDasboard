using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace DynamicDasboardWebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DatabricksProxyController : ControllerBase
    {
        private readonly HttpClient _httpClient;

        public DatabricksProxyController(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        [HttpPost("callLlm")]
        public async Task<IActionResult> CallLlm([FromBody] DatabricksProxyRequest request)
        {
            try
            {
                // Validate the request
                if (string.IsNullOrEmpty(request.ApiToken) ||
                    string.IsNullOrEmpty(request.DatabricksHost) ||
                    string.IsNullOrEmpty(request.EndpointName) ||
                    string.IsNullOrEmpty(request.UserPrompt))
                {
                    return BadRequest(new DatabricksProxyResponse
                    {
                        Success = false,
                        ErrorMessage = "Missing required parameters"
                    });
                }

                // Build the target URL
                string apiUrl = $"https://{request.DatabricksHost}/serving-endpoints/{request.EndpointName}/invocations";

                // Prepare the request with all model parameters
                var chatRequest = new
                {
                    messages = new[]
                    {
                        new { role = "system", content = request.SystemPrompt },
                        new { role = "user", content = request.UserPrompt }
                    },
                    model = request.ModelName,
                    max_tokens = request.MaxTokens,
                    temperature = request.Temperature,
                    top_p = request.TopP,
                    frequency_penalty = request.FrequencyPenalty,
                    presence_penalty = request.PresencePenalty,
                    stop = request.StopSequences?.Length > 0 ? request.StopSequences : null
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(chatRequest),
                    Encoding.UTF8,
                    "application/json"
                );

                // Set authentication
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", request.ApiToken);

                // Log request
                Console.WriteLine($"Sending request to {apiUrl}");
                Console.WriteLine(JsonSerializer.Serialize(chatRequest, new JsonSerializerOptions { WriteIndented = true }));

                // Send the request
                var response = await _httpClient.PostAsync(apiUrl, content);

                // Process response
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        var databricksResponse = JsonSerializer.Deserialize<DatabricksLlmResponse>(responseContent, options);

                        if (databricksResponse?.Choices?.Length > 0)
                        {
                            return Ok(new DatabricksProxyResponse
                            {
                                Success = true,
                                Content = databricksResponse.Choices[0].Message.Content,
                                RawResponse = responseContent
                            });
                        }
                        else
                        {
                            return Ok(new DatabricksProxyResponse
                            {
                                Success = false,
                                ErrorMessage = "No content in response",
                                RawResponse = responseContent
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        return StatusCode(500, new DatabricksProxyResponse
                        {
                            Success = false,
                            ErrorMessage = $"Error parsing response: {ex.Message}",
                            RawResponse = responseContent
                        });
                    }
                }
                else
                {
                    return StatusCode((int)response.StatusCode, new DatabricksProxyResponse
                    {
                        Success = false,
                        ErrorMessage = $"Error from Databricks API: {response.StatusCode}",
                        RawResponse = responseContent
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new DatabricksProxyResponse
                {
                    Success = false,
                    ErrorMessage = $"Server error: {ex.Message}"
                });
            }
        }


        public class DatabricksProxyRequest
        {
            public string DatabricksHost { get; set; }
            public string EndpointName { get; set; }
            public string ApiToken { get; set; }
            public string ModelName { get; set; }
            public int MaxTokens { get; set; }
            public string SystemPrompt { get; set; }
            public string UserPrompt { get; set; }
            public double Temperature { get; set; } = 0.1;
            public double TopP { get; set; } = 0.95;
            public double FrequencyPenalty { get; set; } = 0.0;
            public double PresencePenalty { get; set; } = 0.0;
            public string[] StopSequences { get; set; } = Array.Empty<string>();
        }

        public class DatabricksProxyResponse
        {
            public bool Success { get; set; }
            public string Content { get; set; }
            public string ErrorMessage { get; set; }
            public string RawResponse { get; set; }
        }

        public class DatabricksLlmResponse
        {
            public Choice[] Choices { get; set; }
        }

        public class Choice
        {
            public Message Message { get; set; }
        }

        public class Message
        {
            public string Content { get; set; }
        }
    }
}