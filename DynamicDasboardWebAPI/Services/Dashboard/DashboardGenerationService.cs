using DynamicDasboardWebAPI.Services.LLM;
using System;
using DynamicDashboardCommon.Models;
using DynamicDashboardCommon.Helpers;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using DynamicDashboardCommon.Enums;
using Microsoft.Extensions.Configuration;

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
        private readonly IConfiguration _configuration;
        private readonly bool _useMockData;

        public DashboardGenerationService(
            ILLMService llmService,
            DatabaseSchemaService schemaService,
            ILogsService logsService,
            string templatesFilePath,
            IConfiguration configuration)
        {
            _llmService = llmService ?? throw new ArgumentNullException(nameof(llmService));
            _schemaService = schemaService ?? throw new ArgumentNullException(nameof(schemaService));
            _logsService = logsService ?? throw new ArgumentNullException(nameof(logsService));
            _templatesFilePath = templatesFilePath ?? throw new ArgumentNullException(nameof(templatesFilePath));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            // Read mock mode from configuration (default: false for production)
            _useMockData = _configuration.GetValue<bool>("Dashboard:UseMockData", false);
        }

        public async Task<List<DashboardModel>> GenerateDashboardSuggestionsAsync(int databaseId)
        {
            return await GenerateDashboardSuggestionsAsync(databaseId, "executive-template");
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

                string response;

                if (_useMockData)
                {
                    // Use mock data for testing
                    response = GetMockLLMResponse(templateId);
                    await Task.Delay(500); // Simulate network delay for realistic testing
                }
                else
                {
                    // Get database schema
                    var database = await _schemaService.GetSchemaObject(databaseId);
                    if (database == null)
                    {
                        throw new ArgumentException($"Schema not found for database ID {databaseId}");
                    }

                    string optimizedSchema = _schemaService.BuildOptimizedSchemaString(database);

                    // Generate with simplified prompt (NO positions from LLM!)
                    var prompt = BuildSimplifiedPrompt(optimizedSchema, template);
                    response = await _llmService.GenerateSchemaAnalysisAsync(prompt);
                }

                return ParseAndApplyTemplate(response, databaseId, template);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        /// <summary>
        /// Returns mock LLM response based on template ID for testing
        /// </summary>
        private string GetMockLLMResponse(string templateId)
        {
            return templateId switch
            {
                "operational-template" => GetOperationalTemplateMockData(),
                "executive-template" => GetExecutiveTemplateMockData(),
                "performance-template" => GetPerformanceTemplateMockData(),
                _ => GetOperationalTemplateMockData() // Default
            };
        }

        /// <summary>
        /// Mock data for Operational Template (8 components)
        /// </summary>
        private string GetOperationalTemplateMockData()
        {
            return @"{
  ""components"": [
    {
      ""slot"": 1,
      ""title"": ""Active Traffic Cameras"",
      ""description"": ""Number of currently active traffic cameras"",
      ""queryText"": ""SELECT COUNT(*) as value FROM traffic_cameras WHERE is_active = 1"",
      ""queryIntent"": ""Count of active traffic cameras for monitoring""
    },
    {
      ""slot"": 2,
      ""title"": ""Paid Violations Rate"",
      ""description"": ""Percentage of traffic violations that have been paid"",
      ""queryText"": ""SELECT ROUND((COUNT(CASE WHEN is_paid = 1 THEN 1 END) * 100.0 / COUNT(*)), 2) as value FROM violation_records"",
      ""queryIntent"": ""Percentage of violation fines that have been paid vs total violations""
    },
    {
      ""slot"": 3,
      ""title"": ""Average Incident Processing Time"",
      ""description"": ""Average time between incident creation and current status update"",
      ""queryText"": ""SELECT AVG(DATEDIFF(DAY, created_at, GETDATE())) as value FROM traffic_incidents WHERE status_id IS NOT NULL"",
      ""queryIntent"": ""Average number of days incidents have been in the system with a status""
    },
    {
      ""slot"": 4,
      ""title"": ""Pending Traffic Incidents"",
      ""description"": ""Number of traffic incidents awaiting investigation or resolution"",
      ""queryText"": ""SELECT COUNT(*) as value FROM traffic_incidents ti JOIN incident_status ist ON ti.status_id = ist.status_id WHERE ist.status_code IN ('PENDING', 'INVESTIGATING')"",
      ""queryIntent"": ""Count of incidents that are still pending or under investigation""
    },
    {
      ""slot"": 5,
      ""title"": ""Violations by Severity Level"",
      ""description"": ""Number of violations grouped by severity level"",
      ""queryText"": ""SELECT vt.severity_level as category, COUNT(vr.record_id) as count FROM violation_records vr JOIN violation_types vt ON vr.violation_type_id = vt.violation_type_id GROUP BY vt.severity_level ORDER BY COUNT(vr.record_id) DESC"",
      ""queryIntent"": ""Distribution of violations across different severity levels for performance analysis""
    },
    {
      ""slot"": 6,
      ""title"": ""Traffic Incident Status Distribution"",
      ""description"": ""Distribution of traffic incidents by current status"",
      ""queryText"": ""SELECT ist.status_name as status, COUNT(ti.incident_id) as count FROM traffic_incidents ti JOIN incident_status ist ON ti.status_id = ist.status_id WHERE ist.is_active = 1 GROUP BY ist.status_name, ist.status_id ORDER BY COUNT(ti.incident_id) DESC"",
      ""queryIntent"": ""Pie chart showing how incidents are distributed across different status types""
    },
    {
      ""slot"": 7,
      ""title"": ""Daily Violations Trend"",
      ""description"": ""Daily count of traffic violations over the last 30 days"",
      ""queryText"": ""SELECT CAST(violation_date as DATE) as date, COUNT(record_id) as count FROM violation_records WHERE violation_date >= DATEADD(DAY, -30, GETDATE()) GROUP BY CAST(violation_date as DATE) ORDER BY CAST(violation_date as DATE)"",
      ""queryIntent"": ""Line chart showing daily trend of traffic violations over the past month""
    },
    {
      ""slot"": 8,
      ""title"": ""Recent Traffic Violations"",
      ""description"": ""Most recent traffic violation records"",
      ""queryText"": ""SELECT TOP 10 vr.violation_number, CONCAT(d.first_name, ' ', d.last_name) as driver_name, v.plate_number, vt.violation_name, vr.violation_date, vr.fine_amount, vr.is_paid, vr.location FROM violation_records vr JOIN drivers d ON vr.driver_id = d.driver_id JOIN vehicles v ON vr.vehicle_id = v.vehicle_id JOIN violation_types vt ON vr.violation_type_id = vt.violation_type_id ORDER BY vr.violation_date DESC"",
      ""queryIntent"": ""Table showing the most recent traffic violations with key details""
    }
  ]
}";
        }

        /// <summary>
        /// Mock data for Executive Template (7 components)
        /// </summary>
        private string GetExecutiveTemplateMockData()
        {
            return @"{
  ""components"": [
    {
      ""slot"": 1,
      ""title"": ""Total Violations"",
      ""description"": ""Total number of traffic violations recorded"",
      ""queryText"": ""SELECT COUNT(*) as value FROM violation_records"",
      ""queryIntent"": ""Count of all recorded traffic violations""
    },
    {
      ""slot"": 2,
      ""title"": ""Total Revenue"",
      ""description"": ""Total fine amount collected from violations"",
      ""queryText"": ""SELECT SUM(fine_amount) as value FROM violation_records WHERE is_paid = 1"",
      ""queryIntent"": ""Sum of all paid violation fines""
    },
    {
      ""slot"": 3,
      ""title"": ""Collection Rate"",
      ""description"": ""Percentage of fines collected vs issued"",
      ""queryText"": ""SELECT ROUND((COUNT(CASE WHEN is_paid = 1 THEN 1 END) * 100.0 / COUNT(*)), 2) as value FROM violation_records"",
      ""queryIntent"": ""Success rate of fine collection""
    },
    {
      ""slot"": 4,
      ""title"": ""Violation Trend"",
      ""description"": ""Monthly trend of traffic violations"",
      ""queryText"": ""SELECT FORMAT(violation_date, 'yyyy-MM') as date, COUNT(*) as count FROM violation_records WHERE violation_date >= DATEADD(MONTH, -12, GETDATE()) GROUP BY FORMAT(violation_date, 'yyyy-MM') ORDER BY FORMAT(violation_date, 'yyyy-MM')"",
      ""queryIntent"": ""Monthly violation trend over past year""
    },
    {
      ""slot"": 5,
      ""title"": ""Top Violation Types"",
      ""description"": ""Most common violation types"",
      ""queryText"": ""SELECT TOP 5 vt.violation_name as category, COUNT(vr.record_id) as count FROM violation_records vr JOIN violation_types vt ON vr.violation_type_id = vt.violation_type_id GROUP BY vt.violation_name ORDER BY COUNT(vr.record_id) DESC"",
      ""queryIntent"": ""Top 5 most frequent violation types""
    },
    {
      ""slot"": 6,
      ""title"": ""Payment Status"",
      ""description"": ""Distribution of paid vs unpaid violations"",
      ""queryText"": ""SELECT CASE WHEN is_paid = 1 THEN 'Paid' ELSE 'Unpaid' END as status, COUNT(*) as count FROM violation_records GROUP BY is_paid"",
      ""queryIntent"": ""Paid vs unpaid violations distribution""
    },
    {
      ""slot"": 7,
      ""title"": ""Executive Summary"",
      ""description"": ""Key metrics and statistics"",
      ""queryText"": ""SELECT TOP 20 violation_number, violation_date, fine_amount, is_paid FROM violation_records ORDER BY violation_date DESC"",
      ""queryIntent"": ""Recent violations summary""
    }
  ]
}";
        }

        /// <summary>
        /// Mock data for Performance Template (6 components)
        /// </summary>
        private string GetPerformanceTemplateMockData()
        {
            return @"{
  ""components"": [
    {
      ""slot"": 1,
      ""title"": ""Total Incidents Processed"",
      ""description"": ""Total number of incidents handled"",
      ""queryText"": ""SELECT COUNT(*) as value FROM traffic_incidents"",
      ""queryIntent"": ""Total incident count""
    },
    {
      ""slot"": 2,
      ""title"": ""Average Response Time"",
      ""description"": ""Average time to respond to incidents"",
      ""queryText"": ""SELECT AVG(DATEDIFF(HOUR, created_at, updated_at)) as value FROM traffic_incidents WHERE updated_at IS NOT NULL"",
      ""queryIntent"": ""Average hours to first response""
    },
    {
      ""slot"": 3,
      ""title"": ""Incident Resolution Trend"",
      ""description"": ""Monthly incident resolution rate"",
      ""queryText"": ""SELECT FORMAT(created_at, 'yyyy-MM') as date, COUNT(*) as count FROM traffic_incidents WHERE status_id IN (SELECT status_id FROM incident_status WHERE status_code = 'RESOLVED') GROUP BY FORMAT(created_at, 'yyyy-MM') ORDER BY FORMAT(created_at, 'yyyy-MM')"",
      ""queryIntent"": ""Monthly resolved incidents trend""
    },
    {
      ""slot"": 4,
      ""title"": ""Officer Performance"",
      ""description"": ""Top performing officers by violations issued"",
      ""queryText"": ""SELECT TOP 10 o.badge_number, CONCAT(o.first_name, ' ', o.last_name) as officer_name, COUNT(vr.record_id) as count FROM officers o JOIN violation_records vr ON o.officer_id = vr.officer_id GROUP BY o.badge_number, o.first_name, o.last_name ORDER BY COUNT(vr.record_id) DESC"",
      ""queryIntent"": ""Top 10 officers by violation count""
    },
    {
      ""slot"": 5,
      ""title"": ""Incident Type Distribution"",
      ""description"": ""Distribution by incident type"",
      ""queryText"": ""SELECT incident_type, COUNT(*) as count FROM traffic_incidents GROUP BY incident_type ORDER BY COUNT(*) DESC"",
      ""queryIntent"": ""Incidents grouped by type""
    },
    {
      ""slot"": 6,
      ""title"": ""Performance Details"",
      ""description"": ""Detailed performance metrics"",
      ""queryText"": ""SELECT TOP 15 incident_id, incident_type, created_at, status_id FROM traffic_incidents ORDER BY created_at DESC"",
      ""queryIntent"": ""Recent incidents details""
    }
  ]
}";
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
                // Strip markdown code fences if present
                string cleanJson = llmResponse.Trim();

                // Remove ```json and ``` if present
                if (cleanJson.StartsWith("```"))
                {
                    // Find the first newline after ```
                    int firstNewline = cleanJson.IndexOf('\n');
                    if (firstNewline > 0)
                    {
                        cleanJson = cleanJson.Substring(firstNewline + 1);
                    }

                    // Remove trailing ```
                    if (cleanJson.EndsWith("```"))
                    {
                        cleanJson = cleanJson.Substring(0, cleanJson.LastIndexOf("```"));
                    }

                    cleanJson = cleanJson.Trim();
                }

                var jsonDoc = JsonDocument.Parse(cleanJson);
                var componentsArray = jsonDoc.RootElement.GetProperty("components");

                var dashboard = new DashboardModel
                {
                    Title = template.Name,
                    Description = template.Description,
                    DatabaseID = databaseId,
                    CategoryID = 1,
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
                throw new Exception($"Failed to parse LLM response: {ex.Message}. Response: {llmResponse}", ex);
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
