using DynamicDasboardWebAPI.Services.LLM;
using System;
using DynamicDashboardCommon.Models;
using DynamicDashboardCommon.Helpers;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using DynamicDashboardCommon.Enums;

namespace DynamicDasboardWebAPI.Services
{
    /// <summary>
    /// Service for generating dashboard suggestions using LLM with template support.
    /// </summary>
    public class DashboardGenerationService : IDashboardGenerationService
    {
        private readonly ILLMService _llmService;
        private readonly DatabaseSchemaService _schemaService;
        private readonly ILogsService _logsService;
        private readonly string _templatesFilePath;

        public DashboardGenerationService(
            ILLMService llmService,
            DatabaseSchemaService schemaService,
            ILogsService logsService,
            string templatesFilePath)
        {
            _llmService = llmService ?? throw new ArgumentNullException(nameof(llmService));
            _schemaService = schemaService ?? throw new ArgumentNullException(nameof(schemaService));
            _logsService = logsService ?? throw new ArgumentNullException(nameof(logsService));
            _templatesFilePath = templatesFilePath ?? throw new ArgumentNullException(nameof(templatesFilePath));
        }

        public async Task<List<DashboardModel>> GenerateDashboardSuggestionsAsync(int databaseId)
        {
            return await GenerateDashboardSuggestionsAsync(databaseId, "executive-standard");
        }

        public async Task<List<DashboardModel>> GenerateDashboardSuggestionsAsync(int databaseId, string templateId)
        {
            try
            {
                // Load template
                var template = DashboardTemplateHelper.GetTemplateById(templateId, _templatesFilePath);
                if (template == null)
                {
                    throw new ArgumentException($"Template '{templateId}' not found");
                }

                // Get database schema
                var database = await _schemaService.GetSchemaObject(databaseId);
                if (database == null)
                {
                    throw new ArgumentException($"Schema not found for database ID {databaseId}");
                }

                string optimizedSchema = _schemaService.BuildOptimizedSchemaString(database);

                // Generate with simplified prompt (NO positions from LLM!)
                var prompt = BuildSimplifiedPrompt(optimizedSchema, template);
                var response = await _llmService.GenerateSchemaAnalysisAsync(prompt);

                return ParseAndApplyTemplate(response, databaseId, template);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        /// <summary>
        /// Builds simplified prompt - LLM only generates SQL, not positions
        /// </summary>
        private string BuildSimplifiedPrompt(string schema, DashboardTemplate template)
        {
            var componentRequirements = new List<string>();

            foreach (var slot in template.Components.OrderBy(c => c.Slot))
            {
                string req = $"Slot {slot.Slot}: ";

                if (slot.Type == "kpi")
                {
                    req += $"Generate SQL for KPI '{slot.Title}' - {slot.QueryIntent}. Use {slot.SuggestedAggregation ?? "COUNT"}.";
                }
                else if (slot.Type == "chart")
                {
                    req += $"Generate SQL for {slot.ChartType.ToUpper()} chart '{slot.Title}' - {slot.QueryIntent}.";
                }
                else if (slot.Type == "table")
                {
                    req += $"Generate SQL for data table '{slot.Title}' - {slot.QueryIntent}. Include 5-8 relevant columns.";
                }

                componentRequirements.Add(req);
            }

            return $@"
You are a BI expert creating SQL queries for a dashboard.

# Database Schema
{schema}

# Generate SQL for these {template.Components.Count} components:
{string.Join("\n", componentRequirements)}

# Response Format - SIMPLE JSON (no positions needed):
{{
  ""components"": [
    {{
      ""slot"": 1,
      ""title"": ""Exact title or improved title"",
      ""description"": ""Brief description"",
      ""queryText"": ""SELECT COUNT(*) as value FROM TableName"",
      ""queryIntent"": ""What this shows""
    }}
  ]
}}

RULES:
- Return exactly {template.Components.Count} components (slots 1-{template.Components.Count})
- SQL must be valid and use actual tables/columns from schema
- For KPIs: return single value with alias 'value'
- For charts: return data suitable for visualization
- For tables: SELECT relevant columns (5-8 max)
- NO positions, NO layout info - just SQL!
- Valid JSON only, no markdown
";
        }

        /// <summary>
        /// Parse LLM response and apply template positions
        /// </summary>
        private List<DashboardModel> ParseAndApplyTemplate(string llmResponse, int databaseId, DashboardTemplate template)
        {
            try
            {
                var jsonDoc = JsonDocument.Parse(llmResponse);
                var componentsArray = jsonDoc.RootElement.GetProperty("components");

                var dashboard = new DashboardModel
                {
                    Title = template.Name,
                    Description = template.Description,
                    DatabaseID = databaseId,
                    CategoryID = 1, // Default
                    CreatedAt = DateTime.UtcNow,
                    LastUpdated = DateTime.UtcNow,
                    IsAIGenerated = true,
                    ValidationStatus = DashboardValidationStatus.Draft,
                    SharingStatus = DashboardSharingStatus.Private,
                    Components = new List<DashboardComponent>()
                };

                // Map LLM components to template
                foreach (var llmComp in componentsArray.EnumerateArray())
                {
                    int slot = llmComp.GetProperty("slot").GetInt32();

                    // Find matching template slot
                    var templateSlot = template.Components.FirstOrDefault(s => s.Slot == slot);
                    if (templateSlot == null) continue;

                    var component = new DashboardComponent
                    {
                        ComponentID = slot,
                        Title = GetJsonString(llmComp, "title") ?? templateSlot.Title,
                        Description = GetJsonString(llmComp, "description") ?? templateSlot.Description,
                        QueryText = GetJsonString(llmComp, "queryText"),
                        QueryIntent = GetJsonString(llmComp, "queryIntent") ?? templateSlot.QueryIntent,

                        // Data viewing type from template
                        DataViewingTypeID = DashboardTemplateHelper.GetDataViewingTypeFromSlotType(templateSlot.Type),

                        // POSITIONS FROM TEMPLATE - Simple copy!
                        GridX = templateSlot.GridX,
                        GridY = templateSlot.GridY,
                        GridWidth = templateSlot.GridWidth,
                        GridHeight = templateSlot.GridHeight,

                        IsAIGenerated = true,
                        IsValidated = false,
                        IsVisible = true,
                        CreatedAt = DateTime.UtcNow,
                        LastUpdated = DateTime.UtcNow
                    };

                    // Add visualization config from template
                    if (templateSlot.ChartType != null)
                    {
                        component.VisualizationConfig = JsonSerializer.Serialize(new { chartType = templateSlot.ChartType });
                    }

                    dashboard.Components.Add(component);
                }

                return new List<DashboardModel> { dashboard };
            }
            catch (JsonException ex)
            {
                throw new Exception($"Failed to parse LLM response: {ex.Message}", ex);
            }
        }

        private string GetJsonString(JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String
                ? prop.GetString()
                : null;
        }
    }
}
