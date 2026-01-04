using DynamicDashboardCommon.Enums;
using DynamicDashboardCommon.Models;
using DynamicDasboardWebAPI.Services.LLM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DynamicDasboardWebAPI.Services
{
    /// <summary>
    /// Service for AI assistant operations with LLM integration
    /// </summary>
    public class AssistantService : IAssistantService
    {
        private readonly ILogsService _logsService;
        private readonly ILLMService _llmService;
        private readonly DatabaseSchemaService _schemaService;

        public AssistantService(
            ILogsService logsService,
            ILLMService llmService,
            DatabaseSchemaService schemaService)
        {
            _logsService = logsService ?? throw new ArgumentNullException(nameof(logsService));
            _llmService = llmService ?? throw new ArgumentNullException(nameof(llmService));
            _schemaService = schemaService ?? throw new ArgumentNullException(nameof(schemaService));
        }

        public async Task<AssistantSuggestionResponse> GenerateSuggestionsAsync(AssistantChatRequest request)
        {
            try
            {
                if (request.DashboardId <= 0)
                {
                    return new AssistantSuggestionResponse
                    {
                        Success = false,
                        Message = "Invalid dashboard ID",
                        Suggestions = new List<ComponentSuggestion>()
                    };
                }

                if (request.DatabaseId <= 0)
                {
                    return new AssistantSuggestionResponse
                    {
                        Success = false,
                        Message = "Invalid database ID",
                        Suggestions = new List<ComponentSuggestion>()
                    };
                }

                var schema = await _schemaService.GetSchemaObject(request.DatabaseId, useCache: true);
                if (schema == null)
                {
                    return new AssistantSuggestionResponse
                    {
                        Success = false,
                        Message = "Could not retrieve database schema",
                        Suggestions = new List<ComponentSuggestion>()
                    };
                }

                var systemPrompt = BuildSystemPrompt();
                var userPrompt = BuildUserPrompt(request, schema);

                // ✅ FIXED: Use the CORRECT method for dashboard suggestions
                var llmResponse = await _llmService.GenerateDashboardSuggestionsAsync(
                    systemPrompt,
                    userPrompt
                );

                if (string.IsNullOrWhiteSpace(llmResponse))
                {
                    return new AssistantSuggestionResponse
                    {
                        Success = false,
                        Message = "LLM returned empty response",
                        Suggestions = new List<ComponentSuggestion>()
                    };
                }

                var suggestions = ParseLLMResponse(llmResponse);

                return new AssistantSuggestionResponse
                {
                    Success = true,
                    Message = suggestions.Any()
                        ? $"Found {suggestions.Count} suggestions for your dashboard"
                        : "Your dashboard looks great! No suggestions at this time.",
                    Suggestions = suggestions
                };
            }
            catch (Exception ex)
            {
                return new AssistantSuggestionResponse
                {
                    Success = false,
                    Message = $"Error generating suggestions: {ex.Message}",
                    Suggestions = new List<ComponentSuggestion>()
                };
            }
        }

        private string BuildSystemPrompt()
        {
            return @"You are an expert business intelligence analyst, data analyst, and dashboard designer. Your role is to analyze database schemas and suggest valuable dashboard components that provide business insights.

When suggesting components:
1. Consider what components already exist to avoid duplication
2. Suggest components that add real analytical value based on business intelligence principles
3. Provide SQL queries that are optimized and correct
4. Choose appropriate visualization types for the data
5. Consider business metrics and KPIs that matter
6. Think like a BI expert - focus on actionable insights

Output Format:
Return ONLY a valid JSON array of suggestions. Each suggestion must have:
- title: Component title (concise, business-friendly)
- description: What business insight this provides (1 sentence)
- icon: Font Awesome icon name (without 'fa-' prefix, e.g., 'chart-pie', 'dollar-sign')
- dataViewingTypeID: 1=Label, 2=Table, 3=Number/KPI, 4=Chart
- chartType: ""bar"", ""line"", ""pie"", ""donut"", ""area"", or null for KPI/Table
- sqlTemplate: Valid SQL query that returns data
- gridWidth: 3-12 (column span in 12-column grid)
- gridHeight: 2-4 (row span)

Rules:
- Return maximum 5 suggestions
- If dashboard is complete, return empty array []
- Ensure SQL is valid for the database type
- Focus on actionable business insights, not just data display
- Avoid duplicating existing components
- Use business-friendly titles and descriptions

Example output:
[
  {
    ""title"": ""Revenue Growth Rate"",
    ""description"": ""Track month-over-month revenue growth percentage"",
    ""icon"": ""chart-line"",
    ""dataViewingTypeID"": 4,
    ""chartType"": ""line"",
    ""sqlTemplate"": ""SELECT MONTH(SaleDate) as Month, SUM(Amount) as Revenue FROM Sales GROUP BY MONTH(SaleDate) ORDER BY Month"",
    ""gridWidth"": 6,
    ""gridHeight"": 3
  }
]";
        }

        private string BuildUserPrompt(AssistantChatRequest request, DatabaseSchema schema)
        {
            var prompt = new StringBuilder();

            prompt.AppendLine("Analyze this dashboard and suggest improvements:");
            prompt.AppendLine();

            // Database info
            prompt.AppendLine("DATABASE SCHEMA:");
            prompt.AppendLine($"Database: {schema.Name}");
            prompt.AppendLine("Tables:");

            foreach (var table in schema.Tables.Take(10))
            {
                prompt.AppendLine($"- {table.DBName ?? table.FriendlyName}");
                if (table.Columns != null && table.Columns.Any())
                {
                    var columns = table.Columns.Take(15).Select(c => $"{c.DBName ?? c.FriendlyName} ({c.DataType})");
                    prompt.AppendLine($"  Columns: {string.Join(", ", columns)}");
                }
            }

            prompt.AppendLine();

            // Current dashboard state
            var components = request.CurrentComponents ?? new List<DashboardComponent>();
            prompt.AppendLine("CURRENT DASHBOARD:");
            prompt.AppendLine($"- Total Components: {components.Count}");
            prompt.AppendLine();

            // Existing components with details INCLUDING SQL
            if (components.Any())
            {
                prompt.AppendLine("EXISTING COMPONENTS:");
                for (int i = 0; i < components.Count; i++)
                {
                    var comp = components[i];
                    var typeName = GetComponentTypeName(comp.DataViewingTypeID);
                    var chartInfo = !string.IsNullOrEmpty(comp.ChartType) ? $" ({comp.ChartType})" : "";

                    prompt.AppendLine($"{i + 1}. \"{comp.Title}\" - {typeName}{chartInfo}");

                    if (!string.IsNullOrEmpty(comp.Description))
                    {
                        prompt.AppendLine($"   Description: {comp.Description}");
                    }

                    // SQL query for each component
                    if (!string.IsNullOrEmpty(comp.QueryText))
                    {
                        prompt.AppendLine($"   SQL Query: {comp.QueryText}");
                    }
                }
                prompt.AppendLine();
            }

            // Task
            prompt.AppendLine("TASK:");
            prompt.AppendLine("Suggest up to 5 NEW components that would add the most value to this dashboard based on the dashboard structure and business theme. Focus on:");
            prompt.AppendLine("1. Key business metrics (revenue, growth, conversion)");
            prompt.AppendLine("2. Trend analysis (time-based insights)");
            prompt.AppendLine("3. Distribution analysis (categories, segments)");
            prompt.AppendLine("4. Performance monitoring (KPIs, benchmarks)");
            prompt.AppendLine();
            prompt.AppendLine("Return suggestions as JSON array following the format specified in the system prompt.");

            return prompt.ToString();
        }

        private List<ComponentSuggestion> ParseLLMResponse(string llmResponse)
        {
            try
            {
                var cleanedResponse = llmResponse.Trim();

                if (cleanedResponse.StartsWith("```json"))
                {
                    cleanedResponse = cleanedResponse.Substring(7);
                }
                else if (cleanedResponse.StartsWith("```"))
                {
                    cleanedResponse = cleanedResponse.Substring(3);
                }

                if (cleanedResponse.EndsWith("```"))
                {
                    cleanedResponse = cleanedResponse.Substring(0, cleanedResponse.Length - 3);
                }

                cleanedResponse = cleanedResponse.Trim();

                var startIndex = cleanedResponse.IndexOf('[');
                var endIndex = cleanedResponse.LastIndexOf(']');

                if (startIndex >= 0 && endIndex > startIndex)
                {
                    cleanedResponse = cleanedResponse.Substring(startIndex, endIndex - startIndex + 1);
                }

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var suggestions = JsonSerializer.Deserialize<List<ComponentSuggestion>>(cleanedResponse, options);

                return suggestions?
                    .Where(s => !string.IsNullOrEmpty(s.Title) && !string.IsNullOrEmpty(s.SqlTemplate))
                    .Take(5)
                    .ToList() ?? new List<ComponentSuggestion>();
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Failed to parse LLM response as JSON: {ex.Message}");
                Console.WriteLine($"Response was: {llmResponse}");
                return new List<ComponentSuggestion>();
            }
        }

        private string GetComponentTypeName(int typeId)
        {
            return typeId switch
            {
                1 => "Label",
                2 => "Table",
                3 => "KPI/Number",
                4 => "Chart",
                _ => "Unknown"
            };
        }
    }
}