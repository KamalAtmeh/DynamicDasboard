using DynamicDasboardWebAPI.Services.LLM;
using System;
using DynamicDashboardCommon.Models;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using DynamicDashboardCommon.Enums;

namespace DynamicDasboardWebAPI.Services
{
    /// <summary>
    /// Service for generating dashboard suggestions using LLM.
    /// </summary>
    public class DashboardGenerationService
    {
        private readonly ILLMService _llmService;
        private readonly DatabaseSchemaService _schemaService;
        private readonly ILogsService _logsService;

        /// <summary>
        /// Initializes a new instance of the <see cref="DashboardGenerationService"/> class.
        /// </summary>
        /// <param name="llmService">The LLM service.</param>
        /// <param name="schemaService">The schema service.</param>
        /// <param name="logsService">The logs service.</param>
        public DashboardGenerationService(
            ILLMService llmService,
            DatabaseSchemaService schemaService,
            ILogsService logsService)
        {
            _llmService = llmService ?? throw new ArgumentNullException(nameof(llmService));
            _schemaService = schemaService ?? throw new ArgumentNullException(nameof(schemaService));
            _logsService = logsService ?? throw new ArgumentNullException(nameof(logsService));
        }

        /// <summary>
        /// Generates dashboard suggestions based on database schema.
        /// </summary>
        /// <param name="databaseId">The database ID to analyze.</param>
        /// <returns>A list of suggested dashboards.</returns>
        public async Task<List<DynamicDashboardCommon.Models.DashboardModel>> GenerateDashboardSuggestionsAsync(int databaseId)
        {
            try
            {
                // Get database schema
                var database = await _schemaService.GetSchemaObject(databaseId);
                if (database == null)
                {
                    throw new ArgumentException($"Schema not found for database ID {databaseId}");
                }

                // Optimize schema for LLM (reduce tokens)
                string optimizedSchema = _schemaService.BuildOptimizedSchemaString(database);

                // Generate dashboard suggestions using LLM
                var prompt = BuildDashboardGenerationPrompt(optimizedSchema);
                var response = await _llmService.GenerateSchemaAnalysisAsync(prompt);

                // Parse response to get dashboard suggestions
                return ParseDashboardSuggestions(response, databaseId);
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        /// <summary>
        /// Builds the prompt for dashboard generation.
        /// </summary>
        /// <param name="schema">The database schema string.</param>
        /// <returns>The prompt for the LLM.</returns>
        private string BuildDashboardGenerationPrompt(string schema)
        {
            return $@"
You are a business intelligence expert tasked with designing useful dashboards based on a database schema.
Your job is to recommend business dashboards that would provide valuable insights for this data model.

# Database Schema
{schema}

# Instructions
1. Analyze the schema and identify different business domains or areas that could benefit from dedicated dashboards.
2. For each domain, create a dashboard proposal with:
   - Dashboard title
   - Business purpose and value
   - List of 3-6 visualization components that would be useful
   - For each component, provide:
     - A title
     - A description of what it shows
     - The visualization type (chart, table, KPI, etc.)
     - The SQL query that would generate the data

3. Return your response in the following JSON format:
```json
{{
  ""dashboards"": [
    {{
      ""title"": ""Dashboard Title"",
      ""description"": ""Dashboard business purpose and value"",
      ""category"": ""Business category (Sales, Finance, Operations, etc.)"",
      ""components"": [
        {{
          ""title"": ""Component Title"",
          ""description"": ""What this component shows and its business value"",
          ""dataViewingType"": ""Chart"", // Chart, Table, KPI, Label, Card
          ""queryIntent"": ""Natural language explanation of what data this shows"",
          ""queryText"": ""SELECT ... FROM ... WHERE ..."",
          ""gridWidth"": 6,
          ""gridHeight"": 4,
          ""visualizationConfig"": {{
            ""chartType"": ""bar"", // For chart types: bar, line, pie, etc.
            ""xAxis"": ""Column name for X axis"",
            ""yAxis"": ""Column name for Y axis"",
            ""colorBy"": ""Optional column for color differentiation""
          }}
        }}
      ]
    }}
  ]

4. All SQL queries are valid and reference existing tables and columns from the schema
5. Each dashboard addresses a cohesive business area
6. Components within a dashboard provide complementary information
7. Visualizations are appropriate for the data they represent
8. The JSON is properly formatted and valid";
        }




        /// <summary>
        /// Parses the LLM response to extract dashboard suggestions.
        /// </summary>
        /// <param name="llmResponse">The LLM response.</param>
        /// <param name="databaseId">The database ID.</param>
        /// <returns>A list of suggested dashboards.</returns>
        private List<DashboardModel> ParseDashboardSuggestions(string llmResponse, int databaseId)
        {
            try
            {
                // Extract JSON from response (in case it contains markdown or other text)
                var jsonStart = llmResponse.IndexOf('{');
                var jsonEnd = llmResponse.LastIndexOf('}');

                if (jsonStart >= 0 && jsonEnd > jsonStart)
                {
                    var jsonString = llmResponse.Substring(jsonStart, jsonEnd - jsonStart + 1);
                    var suggestions = JsonSerializer.Deserialize<DashboardSuggestions>(jsonString,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    return MapToDashboards(suggestions, databaseId);
                }

                return new List<DashboardModel>();
            }
            catch (Exception ex)
            {
                // Log error but don't throw - return empty list instead


                return new List<DashboardModel>();
            }
        }

        /// <summary>
        /// Maps suggestions to Dashboard objects.
        /// </summary>
        /// <param name="suggestions">The dashboard suggestions from LLM.</param>
        /// <param name="databaseId">The database ID.</param>
        /// <returns>A list of Dashboard objects.</returns>
        private List<DashboardModel> MapToDashboards(DashboardSuggestions suggestions, int databaseId)
        {
            if (suggestions?.Dashboards == null || !suggestions.Dashboards.Any())
            {
                return new List<DashboardModel>();
            }

            var result = new List<DashboardModel>();

            foreach (var suggestion in suggestions.Dashboards)
            {
                // Create dashboard
                var dashboard = new DashboardModel
                {
                    Title = suggestion.Title,
                    Description = suggestion.Description,
                    DatabaseID = databaseId,
                    // Default values
                    CreatedAt = DateTime.UtcNow,
                    LastUpdated = DateTime.UtcNow,
                    IsAIGenerated = true,
                    ValidationStatus = DashboardValidationStatus.Draft,
                    SharingStatus = DashboardSharingStatus.Private,
                    // Convert category string to ID (would need a lookup in real implementation)
                    CategoryID = 1, // Default category
                    Components = new List<DashboardComponent>()
                };

                // Add components
                if (suggestion.Components != null)
                {
                    int gridY = 0;
                    int gridX = 0;
                    int maxGridWidth = 12; // Assuming 12-column grid layout

                    foreach (var componentSuggestion in suggestion.Components)
                    {
                        // Determine data viewing type
                        int dataViewingTypeId = GetDataViewingTypeId(componentSuggestion.DataViewingType);

                        // Calculate grid position
                        if (gridX + componentSuggestion.GridWidth > maxGridWidth)
                        {
                            gridX = 0;
                            gridY += componentSuggestion.GridHeight;
                        }

                        // Create component
                        var component = new DashboardComponent
                        {
                            Title = componentSuggestion.Title,
                            Description = componentSuggestion.Description,
                            DataViewingTypeID = dataViewingTypeId,
                            QueryIntent = componentSuggestion.QueryIntent,
                            QueryText = componentSuggestion.QueryText,
                            GridX = gridX,
                            GridY = gridY,
                            GridWidth = componentSuggestion.GridWidth,
                            GridHeight = componentSuggestion.GridHeight,
                            IsAIGenerated = true,
                            IsValidated = false,
                            IsVisible = true,
                            CreatedAt = DateTime.UtcNow,
                            LastUpdated = DateTime.UtcNow
                        };

                        // Add visualization config if any
                        if (componentSuggestion.VisualizationConfig != null)
                        {
                            component.VisualizationConfig = JsonSerializer.Serialize(componentSuggestion.VisualizationConfig);
                        }

                        dashboard.Components.Add(component);

                        // Update grid position for next component
                        gridX += componentSuggestion.GridWidth;
                    }
                }

                result.Add(dashboard);
            }

            return result;
        }

        /// <summary>
        /// Gets the data viewing type ID from its name.
        /// </summary>
        /// <param name="typeName">The type name.</param>
        /// <returns>The type ID.</returns>
        private int GetDataViewingTypeId(string typeName)
        {
            return typeName?.ToLower() switch
            {
                "chart" => (int)DataViewingTypeEnum.Chart,
                "table" => (int)DataViewingTypeEnum.Table,
                "kpi" => (int)DataViewingTypeEnum.Number,
                "number" => (int)DataViewingTypeEnum.Number,
                "label" => (int)DataViewingTypeEnum.Label,
                "card" => (int)DataViewingTypeEnum.Card,
                _ => (int)DataViewingTypeEnum.Chart // Default to chart
            };
        }

        #region Helper Classes

        /// <summary>
        /// Root class for dashboard suggestions from LLM.
        /// </summary>
        private class DashboardSuggestions
        {
            public List<DashboardSuggestion> Dashboards { get; set; }
        }

        /// <summary>
        /// Individual dashboard suggestion from LLM.
        /// </summary>
        private class DashboardSuggestion
        {
            public string Title { get; set; }
            public string Description { get; set; }
            public string Category { get; set; }
            public List<ComponentSuggestion> Components { get; set; }
        }

        /// <summary>
        /// Component suggestion from LLM.
        /// </summary>
        private class ComponentSuggestion
        {
            public string Title { get; set; }
            public string Description { get; set; }
            public string DataViewingType { get; set; }
            public string QueryIntent { get; set; }
            public string QueryText { get; set; }
            public int GridWidth { get; set; } = 6;
            public int GridHeight { get; set; } = 4;
            public object VisualizationConfig { get; set; }
        }

        #endregion
    }
}