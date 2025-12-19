//using System;
//using System.Collections.Generic;
//using System.Data;
//using System.Diagnostics;
//using System.Linq;
//using System.Threading.Tasks;
//using Dapper;
//using DynamicDashboardCommon.Enums;
//using DynamicDashboardCommon.Helper;
//using DynamicDashboardCommon.Models;
//using DynamicDasboardWebAPI.Repositories;
//using DynamicDasboardWebAPI.Services.LLM;
//using DynamicDasboardWebAPI.Utilities;
//using Microsoft.AspNetCore.Connections;

//namespace DynamicDasboardWebAPI.Services
//{
//    /// <summary>
//    /// Service for component-related operations in the dashboard builder.
//    /// </summary>
//    public class ComponentService : IComponentService
//    {
//        private readonly DashboardRepository _dashboardRepository;
//        private readonly DatabaseRepository _databaseRepository;
//        private readonly QueryRepository _queryRepository;
//        private readonly LLMServiceFactory _llmServiceFactory;
//        private readonly ILLMService _llmService;


//        /// <summary>
//        /// Initializes a new instance of the ComponentService.
//        /// </summary>
//        public ComponentService(
//            DashboardRepository dashboardRepository,
//            DatabaseRepository databaseRepository,
//            QueryRepository queryRepository,
//        LLMServiceFactory llmServiceFactory,
//)
//        {
//            _dashboardRepository = dashboardRepository ?? throw new ArgumentNullException(nameof(dashboardRepository));
//            _databaseRepository = databaseRepository ?? throw new ArgumentNullException(nameof(databaseRepository));
//            _queryRepository = queryRepository ?? throw new ArgumentNullException(nameof(queryRepository));
//            _llmServiceFactory = llmServiceFactory ?? throw new ArgumentNullException(nameof(llmServiceFactory));


//            _llmService = _llmServiceFactory.CreateLlmService();
//        }

//        #region Component Template Operations

//        /// <inheritdoc/>
//        public async Task<List<ComponentTemplate>> GetAllTemplatesAsync()
//        {
//            // Return predefined templates - in a full implementation, these would come from database
//            return await Task.FromResult(GetPredefinedTemplates());
//        }

//        /// <inheritdoc/>
//        public async Task<List<ComponentTemplate>> GetTemplatesByCategoryAsync(string category)
//        {
//            var templates = await GetAllTemplatesAsync();

//            if (string.IsNullOrWhiteSpace(category) || category.Equals("All", StringComparison.OrdinalIgnoreCase))
//            {
//                return templates;
//            }

//            return templates.Where(t => t.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();
//        }

//        /// <inheritdoc/>
//        public async Task<ComponentTemplate> GetTemplateByIdAsync(int templateId)
//        {
//            var templates = await GetAllTemplatesAsync();
//            return templates.FirstOrDefault(t => t.TemplateID == templateId);
//        }

//        /// <inheritdoc/>
//        public async Task<List<string>> GetTemplateCategoriesAsync()
//        {
//            var templates = await GetAllTemplatesAsync();
//            return templates.Select(t => t.Category).Distinct().OrderBy(c => c).ToList();
//        }

//        /// <inheritdoc/>
//        public async Task<List<ComponentTemplate>> SearchTemplatesAsync(string searchTerm)
//        {
//            var templates = await GetAllTemplatesAsync();

//            if (string.IsNullOrWhiteSpace(searchTerm))
//            {
//                return templates;
//            }

//            var term = searchTerm.ToLower();
//            return templates.Where(t =>
//                t.Title.ToLower().Contains(term) ||
//                t.Description.ToLower().Contains(term) ||
//                t.Category.ToLower().Contains(term) ||
//                (t.Tags != null && t.Tags.Any(tag => tag.ToLower().Contains(term)))
//            ).ToList();
//        }

//        /// <summary>
//        /// Gets predefined component templates.
//        /// </summary>
//        private List<ComponentTemplate> GetPredefinedTemplates()
//        {
//            return new List<ComponentTemplate>
//            {
//                // Data Display Category
//                new ComponentTemplate
//                {
//                    TemplateID = 1,
//                    Title = "Data Table",
//                    Description = "Display data in a tabular format with sorting and pagination",
//                    DataViewingTypeID = (int)DataViewingTypeEnum.Table,
//                    Icon = "fa-table",
//                    Category = "Data Display",
//                    DefaultGridWidth = 6,
//                    DefaultGridHeight = 4,
//                    DisplayOrder = 1,
//                    Tags = new List<string> { "table", "grid", "data", "list" },
//                    VisualizationConfig = new VisualizationConfig
//                    {
//                        ShowHeader = true,
//                        StripedRows = true,
//                        EnableSorting = true,
//                        EnablePagination = true,
//                        PageSize = 10
//                    }.ToJson()
//                },
//                new ComponentTemplate
//                {
//                    TemplateID = 2,
//                    Title = "Info Card",
//                    Description = "Display information in a card format",
//                    DataViewingTypeID = (int)DataViewingTypeEnum.Card,
//                    Icon = "fa-id-card",
//                    Category = "Data Display",
//                    DefaultGridWidth = 4,
//                    DefaultGridHeight = 3,
//                    DisplayOrder = 2,
//                    Tags = new List<string> { "card", "info", "display" }
//                },

//                // Charts Category
//                new ComponentTemplate
//                {
//                    TemplateID = 3,
//                    Title = "Bar Chart",
//                    Description = "Visualize data with vertical or horizontal bars",
//                    DataViewingTypeID = (int)DataViewingTypeEnum.Chart,
//                    Icon = "fa-chart-bar",
//                    Category = "Charts",
//                    DefaultGridWidth = 6,
//                    DefaultGridHeight = 4,
//                    DisplayOrder = 1,
//                    Tags = new List<string> { "chart", "bar", "comparison" },
//                    VisualizationConfig = new VisualizationConfig
//                    {
//                        ChartType = "bar",
//                        ShowLegend = true,
//                        ShowGridLines = true,
//                        EnableAnimation = true
//                    }.ToJson()
//                },
//                new ComponentTemplate
//                {
//                    TemplateID = 4,
//                    Title = "Line Chart",
//                    Description = "Visualize trends over time with lines",
//                    DataViewingTypeID = (int)DataViewingTypeEnum.Chart,
//                    Icon = "fa-chart-line",
//                    Category = "Charts",
//                    DefaultGridWidth = 6,
//                    DefaultGridHeight = 4,
//                    DisplayOrder = 2,
//                    Tags = new List<string> { "chart", "line", "trend", "time" },
//                    VisualizationConfig = new VisualizationConfig
//                    {
//                        ChartType = "line",
//                        ShowLegend = true,
//                        ShowGridLines = true,
//                        EnableAnimation = true
//                    }.ToJson()
//                },
//                new ComponentTemplate
//                {
//                    TemplateID = 5,
//                    Title = "Pie Chart",
//                    Description = "Display proportion of categories",
//                    DataViewingTypeID = (int)DataViewingTypeEnum.Chart,
//                    Icon = "fa-chart-pie",
//                    Category = "Charts",
//                    DefaultGridWidth = 4,
//                    DefaultGridHeight = 4,
//                    DisplayOrder = 3,
//                    Tags = new List<string> { "chart", "pie", "proportion", "percentage" },
//                    VisualizationConfig = new VisualizationConfig
//                    {
//                        ChartType = "pie",
//                        ShowLegend = true,
//                        ShowDataLabels = true
//                    }.ToJson()
//                },
//                new ComponentTemplate
//                {
//                    TemplateID = 6,
//                    Title = "Doughnut Chart",
//                    Description = "Pie chart with a hole in the center",
//                    DataViewingTypeID = (int)DataViewingTypeEnum.Chart,
//                    Icon = "fa-circle-notch",
//                    Category = "Charts",
//                    DefaultGridWidth = 4,
//                    DefaultGridHeight = 4,
//                    DisplayOrder = 4,
//                    Tags = new List<string> { "chart", "doughnut", "donut", "proportion" },
//                    VisualizationConfig = new VisualizationConfig
//                    {
//                        ChartType = "doughnut",
//                        ShowLegend = true,
//                        ShowDataLabels = true
//                    }.ToJson()
//                },
//                new ComponentTemplate
//                {
//                    TemplateID = 7,
//                    Title = "Area Chart",
//                    Description = "Line chart with filled area below",
//                    DataViewingTypeID = (int)DataViewingTypeEnum.Chart,
//                    Icon = "fa-chart-area",
//                    Category = "Charts",
//                    DefaultGridWidth = 6,
//                    DefaultGridHeight = 4,
//                    DisplayOrder = 5,
//                    Tags = new List<string> { "chart", "area", "trend" },
//                    VisualizationConfig = new VisualizationConfig
//                    {
//                        ChartType = "area",
//                        ShowLegend = true,
//                        ShowGridLines = true
//                    }.ToJson()
//                },

//                // KPIs Category
//                new ComponentTemplate
//                {
//                    TemplateID = 8,
//                    Title = "KPI Card",
//                    Description = "Display key metrics with trend indicators",
//                    DataViewingTypeID = (int)DataViewingTypeEnum.Number,
//                    Icon = "fa-tachometer-alt",
//                    Category = "KPIs",
//                    DefaultGridWidth = 3,
//                    DefaultGridHeight = 2,
//                    DisplayOrder = 1,
//                    Tags = new List<string> { "kpi", "metric", "number", "indicator" },
//                    VisualizationConfig = new VisualizationConfig
//                    {
//                        ShowTrend = true,
//                        NumberFormat = "default",
//                        Icon = "fa-chart-line"
//                    }.ToJson()
//                },
//                new ComponentTemplate
//                {
//                    TemplateID = 9,
//                    Title = "Currency KPI",
//                    Description = "Display currency values with formatting",
//                    DataViewingTypeID = (int)DataViewingTypeEnum.Number,
//                    Icon = "fa-dollar-sign",
//                    Category = "KPIs",
//                    DefaultGridWidth = 3,
//                    DefaultGridHeight = 2,
//                    DisplayOrder = 2,
//                    Tags = new List<string> { "kpi", "currency", "money", "revenue" },
//                    VisualizationConfig = new VisualizationConfig
//                    {
//                        ShowTrend = true,
//                        NumberFormat = "currency",
//                        CurrencySymbol = "$",
//                        Icon = "fa-dollar-sign"
//                    }.ToJson()
//                },
//                new ComponentTemplate
//                {
//                    TemplateID = 10,
//                    Title = "Percentage KPI",
//                    Description = "Display percentage values",
//                    DataViewingTypeID = (int)DataViewingTypeEnum.Number,
//                    Icon = "fa-percent",
//                    Category = "KPIs",
//                    DefaultGridWidth = 3,
//                    DefaultGridHeight = 2,
//                    DisplayOrder = 3,
//                    Tags = new List<string> { "kpi", "percentage", "rate" },
//                    VisualizationConfig = new VisualizationConfig
//                    {
//                        ShowTrend = true,
//                        NumberFormat = "percent",
//                        Icon = "fa-percent"
//                    }.ToJson()
//                },

//                // Text Category
//                new ComponentTemplate
//                {
//                    TemplateID = 11,
//                    Title = "Text Label",
//                    Description = "Add titles, descriptions, or notes",
//                    DataViewingTypeID = (int)DataViewingTypeEnum.Label,
//                    Icon = "fa-font",
//                    Category = "Text",
//                    DefaultGridWidth = 6,
//                    DefaultGridHeight = 1,
//                    DisplayOrder = 1,
//                    Tags = new List<string> { "text", "label", "title", "heading" },
//                    VisualizationConfig = new VisualizationConfig
//                    {
//                        FontSize = "medium",
//                        TextAlign = "left",
//                        TextColor = "#2d3748"
//                    }.ToJson()
//                },
//                new ComponentTemplate
//                {
//                    TemplateID = 12,
//                    Title = "Section Header",
//                    Description = "Large header text for sections",
//                    DataViewingTypeID = (int)DataViewingTypeEnum.Label,
//                    Icon = "fa-heading",
//                    Category = "Text",
//                    DefaultGridWidth = 12,
//                    DefaultGridHeight = 1,
//                    DisplayOrder = 2,
//                    Tags = new List<string> { "header", "section", "title" },
//                    VisualizationConfig = new VisualizationConfig
//                    {
//                        FontSize = "xlarge",
//                        TextAlign = "left",
//                        TextColor = "#2d3748",
//                        IsBold = true
//                    }.ToJson()
//                }
//            };
//        }

//        #endregion

//        #region Component CRUD Operations

//        /// <inheritdoc/>
//        public async Task<DashboardComponent> GetComponentByIdAsync(int componentId)
//        {
//            return await _dashboardRepository.GetDashboardComponentByIdAsync(componentId);
//        }

//        /// <inheritdoc/>
//        public async Task<DashboardComponent> CreateComponentAsync(DashboardComponent component)
//        {
//            ValidateComponent(component);

//            component.CreatedAt = DateTime.UtcNow;
//            component.LastUpdated = DateTime.UtcNow;

//            var componentId = await _dashboardRepository.CreateDashboardComponentAsync(component);
//            component.ComponentID = componentId;

//            return component;
//        }

//        /// <inheritdoc/>
//        public async Task<DashboardComponent> UpdateComponentAsync(DashboardComponent component)
//        {
//            ValidateComponent(component);

//            component.LastUpdated = DateTime.UtcNow;

//            await _dashboardRepository.UpdateDashboardComponentAsync(component);

//            return component;
//        }

//        /// <inheritdoc/>
//        public async Task<bool> DeleteComponentAsync(int componentId)
//        {
//            return await _dashboardRepository.DeleteDashboardComponentAsync(componentId);
//        }

//        /// <inheritdoc/>
//        public async Task<DashboardComponent> DuplicateComponentAsync(int componentId, string newTitle = null)
//        {
//            var original = await GetComponentByIdAsync(componentId);
//            if (original == null)
//            {
//                throw new ArgumentException($"Component with ID {componentId} not found.");
//            }

//            var duplicate = new DashboardComponent
//            {
//                DashboardID = original.DashboardID,
//                Title = newTitle ?? $"{original.Title} (Copy)",
//                Description = original.Description,
//                DataViewingTypeID = original.DataViewingTypeID,
//                GridX = original.GridX,
//                GridY = original.GridY + original.GridHeight, // Place below original
//                GridWidth = original.GridWidth,
//                GridHeight = original.GridHeight,
//                QueryText = original.QueryText,
//                QueryIntent = original.QueryIntent,
//                VisualizationConfig = original.VisualizationConfig,
//                FilterExpression = original.FilterExpression,
//                RefreshInterval = original.RefreshInterval,
//                IsVisible = true,
//                IsAIGenerated = original.IsAIGenerated,
//                IsValidated = false // Reset validation for duplicate
//            };

//            // Copy parameters
//            if (original.Parameters?.Any() == true)
//            {
//                duplicate.Parameters = original.Parameters.Select(p => new ComponentParameter
//                {
//                    Name = p.Name,
//                    DisplayName = p.DisplayName,
//                    DefaultValue = p.DefaultValue,
//                    CurrentValue = p.CurrentValue,
//                    DataType = p.DataType,
//                    IsRequired = p.IsRequired,
//                    Options = p.Options,
//                    IsVisible = p.IsVisible,
//                    ValidationRules = p.ValidationRules,
//                    Description = p.Description
//                }).ToList();
//            }

//            return await CreateComponentAsync(duplicate);
//        }

//        /// <summary>
//        /// Validates a component before saving.
//        /// </summary>
//        private void ValidateComponent(DashboardComponent component)
//        {
//            if (component == null)
//            {
//                throw new ArgumentNullException(nameof(component));
//            }

//            if (string.IsNullOrWhiteSpace(component.Title))
//            {
//                throw new ArgumentException("Component title is required.");
//            }

//            if (component.DataViewingTypeID <= 0)
//            {
//                throw new ArgumentException("Valid data viewing type ID is required.");
//            }

//            if (component.GridWidth <= 0 || component.GridWidth > 12)
//            {
//                throw new ArgumentException("Grid width must be between 1 and 12.");
//            }

//            if (component.GridHeight <= 0)
//            {
//                throw new ArgumentException("Grid height must be greater than 0.");
//            }

//            if (component.GridX < 0 || component.GridX > 11)
//            {
//                throw new ArgumentException("Grid X position must be between 0 and 11.");
//            }

//            if (component.GridY < 0)
//            {
//                throw new ArgumentException("Grid Y position must be 0 or greater.");
//            }
//        }

//        #endregion

//        #region Data Source Operations

//        /// <inheritdoc/>
//        public async Task<List<ComponentDataSource>> GetAvailableDataSourcesAsync()
//        {
//            var databases = await _databaseRepository.GetAllDatabasesAsync();

//            return databases
//                .Where(db => db.IsActive)
//                .Select(db => new ComponentDataSource
//                {
//                    Id = db.DatabaseID,
//                    Name = db.Name,
//                    DatabaseType = db.DatabaseType,
//                    IsActive = db.IsActive
//                })
//                .ToList();
//        }

//        /// <inheritdoc/>
//        public async Task<ComponentDataSource> GetDataSourceByIdAsync(int dataSourceId)
//        {
//            var database = await _databaseRepository.GetDatabaseByIdAsync(dataSourceId);
//            if (database == null)
//            {
//                return null;
//            }

//            return new ComponentDataSource
//            {
//                Id = database.DatabaseID,
//                Name = database.Name,
//                DatabaseType = database.DatabaseType,
//                IsActive = database.IsActive
//            };
//        }

//        #endregion

//        #region Query Operations

//        /// <inheritdoc/>
//        public async Task<QueryValidationResponse> ValidateQueryAsync(QueryValidationRequest request)
//        {
//            var response = new QueryValidationResponse();
//            var stopwatch = Stopwatch.StartNew();

//            try
//            {
//                if (string.IsNullOrWhiteSpace(request.QueryText))
//                {
//                    response.IsValid = false;
//                    response.Message = "Query text is required.";
//                    return response;
//                }

//                var database = await _databaseRepository.GetDatabaseByIdAsync(request.DatabaseId);
//                if (database == null)
//                {
//                    response.IsValid = false;
//                    response.Message = "Database not found.";
//                    return response;
//                }

//                // Execute query with limit for validation
//                using var connection = _connectionFactory.CreateConnection(database.ConnectionString, database.DatabaseType);

//                var limitedQuery = AddQueryLimit(request.QueryText, request.MaxSampleRows, database.DatabaseType);

//                var result = await connection.QueryAsync<dynamic>(limitedQuery);
//                var data = result.ToList();

//                response.IsValid = true;
//                response.Message = "Query is valid.";

//                if (request.IncludeSampleData && data.Any())
//                {
//                    response.SampleData = data.Select(row =>
//                    {
//                        var dict = new Dictionary<string, object>();
//                        foreach (var prop in (IDictionary<string, object>)row)
//                        {
//                            dict[prop.Key] = prop.Value;
//                        }
//                        return dict;
//                    }).ToList();

//                    // Extract column info from first row
//                    var firstRow = (IDictionary<string, object>)data.First();
//                    response.Columns = firstRow.Select((kvp, index) => new QueryColumnInfo
//                    {
//                        Name = kvp.Key,
//                        DataType = kvp.Value?.GetType().Name ?? "Unknown",
//                        IsNullable = true,
//                        Ordinal = index
//                    }).ToList();
//                }

//                response.EstimatedRowCount = data.Count;
//            }
//            catch (Exception ex)
//            {
//                response.IsValid = false;
//                response.Message = "Query validation failed.";
//                response.ErrorDetails = ex.Message;
//            }
//            finally
//            {
//                stopwatch.Stop();
//                response.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
//            }

//            return response;
//        }

//        /// <inheritdoc/>
//        public async Task<QueryGenerationResponse> GenerateQueryFromIntentAsync(QueryGenerationRequest request)
//        {
//            var response = new QueryGenerationResponse();

//            try
//            {
//                if (string.IsNullOrWhiteSpace(request.QueryIntent))
//                {
//                    response.Success = false;
//                    response.ErrorMessage = "Query intent is required.";
//                    return response;
//                }

//                var llmService = _llmServiceFactory.Create();

//                // Get database schema for context
//                var database = await _databaseRepository.GetDatabaseByIdAsync(request.DatabaseId);
//                if (database == null)
//                {
//                    response.Success = false;
//                    response.ErrorMessage = "Database not found.";
//                    return response;
//                }

//                // Build prompt for LLM
//                var prompt = BuildQueryGenerationPrompt(request, database);

//                // Call LLM service
//                var llmResponse = await llmService.GenerateSqlQueryAsync(prompt, database.DatabaseType);

//                if (llmResponse != null && !string.IsNullOrWhiteSpace(llmResponse.SqlQuery))
//                {
//                    response.Success = true;
//                    response.GeneratedQuery = llmResponse.SqlQuery;
//                    response.Explanation = llmResponse.BusinessExplanation;
//                }
//                else
//                {
//                    response.Success = false;
//                    response.ErrorMessage = "Failed to generate query from intent.";
//                }
//            }
//            catch (Exception ex)
//            {
//                response.Success = false;
//                response.ErrorMessage = $"Error generating query: {ex.Message}";
//            }

//            return response;
//        }

//        /// <inheritdoc/>
//        public async Task<List<Dictionary<string, object>>> GetComponentSampleDataAsync(int componentId, int maxRows = 10)
//        {
//            var component = await GetComponentByIdAsync(componentId);
//            if (component == null || string.IsNullOrWhiteSpace(component.QueryText))
//            {
//                return new List<Dictionary<string, object>>();
//            }

//            var dashboard = await _dashboardRepository.GetDashboardByIdAsync(component.DashboardID);
//            if (dashboard == null)
//            {
//                return new List<Dictionary<string, object>>();
//            }

//            var request = new QueryValidationRequest
//            {
//                QueryText = component.QueryText,
//                DatabaseId = dashboard.DatabaseID,
//                IncludeSampleData = true,
//                MaxSampleRows = maxRows
//            };

//            var result = await ValidateQueryAsync(request);
//            return result.SampleData ?? new List<Dictionary<string, object>>();
//        }

//        /// <inheritdoc/>
//        public async Task<List<Dictionary<string, object>>> ExecuteComponentQueryAsync(int componentId, Dictionary<string, object> parameters = null)
//        {
//            var component = await GetComponentByIdAsync(componentId);
//            if (component == null || string.IsNullOrWhiteSpace(component.QueryText))
//            {
//                return new List<Dictionary<string, object>>();
//            }

//            var dashboard = await _dashboardRepository.GetDashboardByIdAsync(component.DashboardID);
//            if (dashboard == null)
//            {
//                return new List<Dictionary<string, object>>();
//            }

//            var database = await _databaseRepository.GetDatabaseByIdAsync(dashboard.DatabaseID);
//            if (database == null)
//            {
//                return new List<Dictionary<string, object>>();
//            }

//            using var connection = _connectionFactory.CreateConnection(database.ConnectionString, database.DatabaseType);

//            var result = await connection.QueryAsync<dynamic>(component.QueryText, parameters);

//            return result.Select(row =>
//            {
//                var dict = new Dictionary<string, object>();
//                foreach (var prop in (IDictionary<string, object>)row)
//                {
//                    dict[prop.Key] = prop.Value;
//                }
//                return dict;
//            }).ToList();
//        }

//        /// <summary>
//        /// Adds a limit clause to a query for validation purposes.
//        /// </summary>
//        private string AddQueryLimit(string query, int limit, string databaseType)
//        {
//            query = query.TrimEnd().TrimEnd(';');

//            return databaseType?.ToLower() switch
//            {
//                "sqlserver" => $"SELECT TOP {limit} * FROM ({query}) AS LimitedQuery",
//                "mysql" => $"{query} LIMIT {limit}",
//                "oracle" => $"SELECT * FROM ({query}) WHERE ROWNUM <= {limit}",
//                "postgresql" => $"{query} LIMIT {limit}",
//                _ => $"{query} LIMIT {limit}"
//            };
//        }

//        /// <summary>
//        /// Builds a prompt for query generation.
//        /// </summary>
//        private string BuildQueryGenerationPrompt(QueryGenerationRequest request, Database database)
//        {
//            return $@"Generate a SQL query for {database.DatabaseType} database.
//Intent: {request.QueryIntent}
//Component Type: {GetComponentTypeName(request.DataViewingTypeID)}
//{(string.IsNullOrWhiteSpace(request.AdditionalContext) ? "" : $"Additional Context: {request.AdditionalContext}")}

//Return only the SQL query without any explanation.";
//        }

//        /// <summary>
//        /// Gets the component type name from ID.
//        /// </summary>
//        private string GetComponentTypeName(int dataViewingTypeId)
//        {
//            return dataViewingTypeId switch
//            {
//                (int)DataViewingTypeEnum.Table => "Table",
//                (int)DataViewingTypeEnum.Chart => "Chart",
//                (int)DataViewingTypeEnum.Number => "KPI/Number",
//                (int)DataViewingTypeEnum.Card => "Card",
//                (int)DataViewingTypeEnum.Label => "Label",
//                _ => "Unknown"
//            };
//        }

//        #endregion

//        #region Visualization Config Operations

//        /// <inheritdoc/>
//        public async Task<VisualizationConfig> GetVisualizationConfigAsync(int componentId)
//        {
//            var component = await GetComponentByIdAsync(componentId);
//            if (component == null)
//            {
//                return new VisualizationConfig();
//            }

//            return VisualizationConfig.FromJson(component.VisualizationConfig);
//        }

//        /// <inheritdoc/>
//        public async Task<bool> UpdateVisualizationConfigAsync(int componentId, VisualizationConfig config)
//        {
//            var component = await GetComponentByIdAsync(componentId);
//            if (component == null)
//            {
//                return false;
//            }

//            component.VisualizationConfig = config.ToJson();
//            component.LastUpdated = DateTime.UtcNow;

//            await _dashboardRepository.UpdateDashboardComponentAsync(component);
//            return true;
//        }

//        /// <inheritdoc/>
//        public List<ColorScheme> GetColorSchemes()
//        {
//            return ColorSchemeHelper.GetAllSchemes();
//        }

//        #endregion

//        #region Parameter Operations

//        /// <inheritdoc/>
//        public async Task<List<ComponentParameter>> GetComponentParametersAsync(int componentId)
//        {
//            var component = await GetComponentByIdAsync(componentId);
//            return component?.Parameters ?? new List<ComponentParameter>();
//        }

//        /// <inheritdoc/>
//        public async Task<ComponentParameter> AddComponentParameterAsync(int componentId, ComponentParameter parameter)
//        {
//            parameter.ComponentID = componentId;
//            var parameterId = await _dashboardRepository.CreateComponentParameterAsync(parameter);
//            parameter.ParameterID = parameterId;
//            return parameter;
//        }

//        /// <inheritdoc/>
//        public async Task<ComponentParameter> UpdateComponentParameterAsync(ComponentParameter parameter)
//        {
//            await _dashboardRepository.UpdateComponentParameterAsync(parameter);
//            return parameter;
//        }

//        /// <inheritdoc/>
//        public async Task<bool> DeleteComponentParameterAsync(int parameterId)
//        {
//            return await _dashboardRepository.DeleteComponentParameterAsync(parameterId);
//        }

//        #endregion

//        #region Interaction Operations

//        /// <inheritdoc/>
//        public async Task<List<DashboardComponent>> GetCrossFilterTargetsAsync(int dashboardId, int excludeComponentId)
//        {
//            var dashboard = await _dashboardRepository.GetDashboardByIdAsync(dashboardId);
//            if (dashboard?.Components == null)
//            {
//                return new List<DashboardComponent>();
//            }

//            return dashboard.Components
//                .Where(c => c.ComponentID != excludeComponentId && c.IsVisible)
//                .ToList();
//        }

//        /// <inheritdoc/>
//        public async Task<bool> SetupCrossFilterAsync(int sourceComponentId, List<int> targetComponentIds, string filterField)
//        {
//            var sourceComponent = await GetComponentByIdAsync(sourceComponentId);
//            if (sourceComponent == null)
//            {
//                return false;
//            }

//            var config = VisualizationConfig.FromJson(sourceComponent.VisualizationConfig);
//            config.EnableCrossFilter = true;
//            config.CrossFilterTargets = targetComponentIds;

//            return await UpdateVisualizationConfigAsync(sourceComponentId, config);
//        }

//        #endregion
//    }
//}