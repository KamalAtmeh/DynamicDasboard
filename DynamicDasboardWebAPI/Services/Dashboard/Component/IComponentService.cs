//using System.Collections.Generic;
//using System.Threading.Tasks;
//using DynamicDashboardCommon.Models;

//namespace DynamicDasboardWebAPI.Services
//{
//    /// <summary>
//    /// Interface for component-related operations in the dashboard builder.
//    /// </summary>
//    public interface IComponentService
//    {
//        #region Component Template Operations

//        /// <summary>
//        /// Gets all available component templates.
//        /// </summary>
//        /// <returns>List of component templates.</returns>
//        Task<List<ComponentTemplate>> GetAllTemplatesAsync();

//        /// <summary>
//        /// Gets component templates by category.
//        /// </summary>
//        /// <param name="category">The category name.</param>
//        /// <returns>List of component templates in the category.</returns>
//        Task<List<ComponentTemplate>> GetTemplatesByCategoryAsync(string category);

//        /// <summary>
//        /// Gets a component template by ID.
//        /// </summary>
//        /// <param name="templateId">The template ID.</param>
//        /// <returns>The component template.</returns>
//        Task<ComponentTemplate> GetTemplateByIdAsync(int templateId);

//        /// <summary>
//        /// Gets all template categories.
//        /// </summary>
//        /// <returns>List of category names.</returns>
//        Task<List<string>> GetTemplateCategoriesAsync();

//        /// <summary>
//        /// Searches templates by keyword.
//        /// </summary>
//        /// <param name="searchTerm">The search term.</param>
//        /// <returns>List of matching templates.</returns>
//        Task<List<ComponentTemplate>> SearchTemplatesAsync(string searchTerm);

//        #endregion

//        #region Component CRUD Operations

//        /// <summary>
//        /// Gets a dashboard component by ID.
//        /// </summary>
//        /// <param name="componentId">The component ID.</param>
//        /// <returns>The dashboard component.</returns>
//        Task<DashboardComponent> GetComponentByIdAsync(int componentId);

//        /// <summary>
//        /// Creates a new dashboard component.
//        /// </summary>
//        /// <param name="component">The component to create.</param>
//        /// <returns>The created component with ID.</returns>
//        Task<DashboardComponent> CreateComponentAsync(DashboardComponent component);

//        /// <summary>
//        /// Updates an existing dashboard component.
//        /// </summary>
//        /// <param name="component">The component to update.</param>
//        /// <returns>The updated component.</returns>
//        Task<DashboardComponent> UpdateComponentAsync(DashboardComponent component);

//        /// <summary>
//        /// Deletes a dashboard component.
//        /// </summary>
//        /// <param name="componentId">The component ID to delete.</param>
//        /// <returns>True if successful.</returns>
//        Task<bool> DeleteComponentAsync(int componentId);

//        /// <summary>
//        /// Duplicates an existing component.
//        /// </summary>
//        /// <param name="componentId">The component ID to duplicate.</param>
//        /// <param name="newTitle">Optional new title for the duplicate.</param>
//        /// <returns>The duplicated component.</returns>
//        Task<DashboardComponent> DuplicateComponentAsync(int componentId, string newTitle = null);

//        #endregion

//        #region Data Source Operations

//        /// <summary>
//        /// Gets all available data sources for components.
//        /// </summary>
//        /// <returns>List of data sources.</returns>
//        Task<List<ComponentDataSource>> GetAvailableDataSourcesAsync();

//        /// <summary>
//        /// Gets data source by ID.
//        /// </summary>
//        /// <param name="dataSourceId">The data source ID.</param>
//        /// <returns>The data source.</returns>
//        Task<ComponentDataSource> GetDataSourceByIdAsync(int dataSourceId);

//        #endregion

//        #region Query Operations

//        /// <summary>
//        /// Validates a SQL query against a database.
//        /// </summary>
//        /// <param name="request">The validation request.</param>
//        /// <returns>Validation response with results.</returns>
//        Task<QueryValidationResponse> ValidateQueryAsync(QueryValidationRequest request);

//        /// <summary>
//        /// Generates SQL from natural language intent.
//        /// </summary>
//        /// <param name="request">The generation request.</param>
//        /// <returns>Generated query response.</returns>
//        Task<QueryGenerationResponse> GenerateQueryFromIntentAsync(QueryGenerationRequest request);

//        /// <summary>
//        /// Gets sample data for a component query.
//        /// </summary>
//        /// <param name="componentId">The component ID.</param>
//        /// <param name="maxRows">Maximum rows to return.</param>
//        /// <returns>Sample data as list of dictionaries.</returns>
//        Task<List<Dictionary<string, object>>> GetComponentSampleDataAsync(int componentId, int maxRows = 10);

//        /// <summary>
//        /// Executes a component query and returns data.
//        /// </summary>
//        /// <param name="componentId">The component ID.</param>
//        /// <param name="parameters">Optional query parameters.</param>
//        /// <returns>Query results.</returns>
//        Task<List<Dictionary<string, object>>> ExecuteComponentQueryAsync(int componentId, Dictionary<string, object> parameters = null);

//        #endregion

//        #region Visualization Config Operations

//        /// <summary>
//        /// Gets the visualization configuration for a component.
//        /// </summary>
//        /// <param name="componentId">The component ID.</param>
//        /// <returns>The visualization configuration.</returns>
//        Task<VisualizationConfig> GetVisualizationConfigAsync(int componentId);

//        /// <summary>
//        /// Updates the visualization configuration for a component.
//        /// </summary>
//        /// <param name="componentId">The component ID.</param>
//        /// <param name="config">The new configuration.</param>
//        /// <returns>True if successful.</returns>
//        Task<bool> UpdateVisualizationConfigAsync(int componentId, VisualizationConfig config);

//        /// <summary>
//        /// Gets all available color schemes.
//        /// </summary>
//        /// <returns>List of color schemes.</returns>
//        List<ColorScheme> GetColorSchemes();

//        #endregion

//        #region Parameter Operations

//        /// <summary>
//        /// Gets parameters for a component.
//        /// </summary>
//        /// <param name="componentId">The component ID.</param>
//        /// <returns>List of component parameters.</returns>
//        Task<List<ComponentParameter>> GetComponentParametersAsync(int componentId);

//        /// <summary>
//        /// Adds a parameter to a component.
//        /// </summary>
//        /// <param name="componentId">The component ID.</param>
//        /// <param name="parameter">The parameter to add.</param>
//        /// <returns>The added parameter with ID.</returns>
//        Task<ComponentParameter> AddComponentParameterAsync(int componentId, ComponentParameter parameter);

//        /// <summary>
//        /// Updates a component parameter.
//        /// </summary>
//        /// <param name="parameter">The parameter to update.</param>
//        /// <returns>The updated parameter.</returns>
//        Task<ComponentParameter> UpdateComponentParameterAsync(ComponentParameter parameter);

//        /// <summary>
//        /// Deletes a component parameter.
//        /// </summary>
//        /// <param name="parameterId">The parameter ID.</param>
//        /// <returns>True if successful.</returns>
//        Task<bool> DeleteComponentParameterAsync(int parameterId);

//        #endregion

//        #region Interaction Operations

//        /// <summary>
//        /// Gets components that can be cross-filter targets.
//        /// </summary>
//        /// <param name="dashboardId">The dashboard ID.</param>
//        /// <param name="excludeComponentId">Component ID to exclude from results.</param>
//        /// <returns>List of potential target components.</returns>
//        Task<List<DashboardComponent>> GetCrossFilterTargetsAsync(int dashboardId, int excludeComponentId);

//        /// <summary>
//        /// Sets up cross-filter relationship between components.
//        /// </summary>
//        /// <param name="sourceComponentId">The source component ID.</param>
//        /// <param name="targetComponentIds">The target component IDs.</param>
//        /// <param name="filterField">The field to filter on.</param>
//        /// <returns>True if successful.</returns>
//        Task<bool> SetupCrossFilterAsync(int sourceComponentId, List<int> targetComponentIds, string filterField);

//        #endregion
//    }
//}