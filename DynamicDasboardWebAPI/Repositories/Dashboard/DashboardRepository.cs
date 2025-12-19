using Dapper;
using DynamicDasboardWebAPI.Utilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using DynamicDashboardCommon.Models;

namespace DynamicDasboardWebAPI.Repositories
{
    /// <summary>
    /// Repository for handling CRUD operations for dashboards.
    /// </summary>
    public class DashboardRepository : BaseRepository
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DashboardRepository"/> class.
        /// </summary>
        /// <param name="appDbConnection">The database connection.</param>
        /// <param name="connectionFactory">The connection factory.</param>
        public DashboardRepository(
            IDbConnection appDbConnection,
            DbConnectionFactory connectionFactory)
            : base(appDbConnection, connectionFactory)
        {
        }

        /// <summary>
        /// Gets all dashboards with optional filtering.
        /// </summary>
        /// <param name="categoryId">Optional category filter.</param>
        /// <param name="createdBy">Optional creator filter.</param>
        /// <param name="sharingStatus">Optional sharing status filter.</param>
        /// <returns>A collection of dashboards.</returns>
        public async Task<IEnumerable<DashboardModel>> GetAllDashboardsAsync(
            int? categoryId = null, int? createdBy = null, DashboardSharingStatus? sharingStatus = null)
        {
            try
            {
                var parameters = new DynamicParameters();
                var whereConditions = new List<string>();

                if (categoryId.HasValue)
                {
                    whereConditions.Add("CategoryID = @CategoryID");
                    parameters.Add("@CategoryID", categoryId.Value);
                }

                if (createdBy.HasValue)
                {
                    whereConditions.Add("CreatedBy = @CreatedBy");
                    parameters.Add("@CreatedBy", createdBy.Value);
                }

                if (sharingStatus.HasValue)
                {
                    whereConditions.Add("SharingStatus = @SharingStatus");
                    parameters.Add("@SharingStatus", (int)sharingStatus.Value);
                }

                var whereClause = whereConditions.Count > 0
                    ? $"WHERE {string.Join(" AND ", whereConditions)}"
                    : string.Empty;

                var sql = $@"
                    SELECT d.*, c.Name as CategoryName
                    FROM Dashboards d
                    LEFT JOIN DashboardCategories c ON d.CategoryID = c.CategoryID
                    {whereClause}
                    ORDER BY d.LastUpdated DESC";

                return await WithConnectionAsync(async conn =>
                {
                    var dashboards = await conn.QuerySafeAsync<DashboardModel>(sql, parameters);

                    // Load components for each dashboard
                    foreach (var dashboard in dashboards)
                    {
                        dashboard.Components = (await GetDashboardComponentsAsync(dashboard.DashboardID)).ToList();

                        // Deserialize tags
                        if (!string.IsNullOrEmpty(dashboard.FiltersConfig))
                        {
                            try
                            {
                                dashboard.Tags = JsonSerializer.Deserialize<List<string>>(dashboard.FiltersConfig);
                            }
                            catch
                            {
                                dashboard.Tags = new List<string>();
                            }
                        }
                    }

                    return dashboards;
                });
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Gets a dashboard by its ID.
        /// </summary>
        /// <param name="dashboardId">The dashboard ID.</param>
        /// <returns>The dashboard.</returns>
        public async Task<DashboardModel> GetDashboardByIdAsync(int dashboardId)
        {
            try
            {
                const string sql = @"
                    SELECT d.*, c.Name as CategoryName
                    FROM Dashboards d
                    LEFT JOIN DashboardCategories c ON d.CategoryID = c.CategoryID
                    WHERE d.DashboardID = @DashboardID";

                return await WithConnectionAsync(async conn =>
                {
                    var dashboard = await conn.QueryFirstOrDefaultSafeAsync<DashboardModel>(sql, new { DashboardID = dashboardId });

                    if (dashboard != null)
                    {
                        // Load components
                        dashboard.Components = (await GetDashboardComponentsAsync(dashboardId)).ToList();

                        // Deserialize tags
                        if (!string.IsNullOrEmpty(dashboard.FiltersConfig))
                        {
                            try
                            {
                                dashboard.Tags = JsonSerializer.Deserialize<List<string>>(dashboard.FiltersConfig);
                            }
                            catch
                            {
                                dashboard.Tags = new List<string>();
                            }
                        }
                    }

                    return dashboard;
                });
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Creates a new dashboard.
        /// </summary>
        /// <param name="dashboard">The dashboard to create.</param>
        /// <returns>The ID of the created dashboard.</returns>
        public async Task<int> CreateDashboardAsync(DashboardModel dashboard)
        {
            try
            {
                // Serialize tags
                if (dashboard.Tags?.Any() == true)
                {
                    dashboard.FiltersConfig = JsonSerializer.Serialize(dashboard.Tags);
                }

                const string sql = @"
                    INSERT INTO Dashboards (
                        Title, Description, LayoutConfig, DatabaseID, CreatedBy,
                        CreatedAt, LastUpdated, IsFeatured, CategoryID, 
                        SharingStatus, RefreshInterval, FiltersConfig, 
                        IsAIGenerated, ValidationStatus
                    ) VALUES (
                        @Title, @Description, @LayoutConfig, @DatabaseID, @CreatedBy,
                        @CreatedAt, @LastUpdated, @IsFeatured, @CategoryID, 
                        @SharingStatus, @RefreshInterval, @FiltersConfig, 
                        @IsAIGenerated, @ValidationStatus
                    );
                    SELECT CAST(SCOPE_IDENTITY() as int)";

                return await WithConnectionAsync(async conn =>
                {
                    using var transaction = conn.BeginTransaction();
                    try
                    {
                        // Insert dashboard
                        var dashboardId = await conn.ExecuteScalarSafeAsync<int>(sql, dashboard, transaction);

                        // Insert components if any
                        if (dashboard.Components?.Any() == true)
                        {
                            foreach (var component in dashboard.Components)
                            {
                                component.DashboardID = dashboardId;
                                await CreateDashboardComponentAsync(component, conn, transaction);
                            }
                        }

                        transaction.Commit();
                        return dashboardId;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                });
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Updates an existing dashboard.
        /// </summary>
        /// <param name="dashboard">The dashboard to update.</param>
        /// <returns>True if successful.</returns>
        public async Task<bool> UpdateDashboardAsync(DashboardModel dashboard)
        {
            try
            {
                // Serialize tags
                if (dashboard.Tags?.Any() == true)
                {
                    dashboard.FiltersConfig = JsonSerializer.Serialize(dashboard.Tags);
                }

                dashboard.LastUpdated = DateTime.UtcNow;

                const string sql = @"
                    UPDATE Dashboards SET
                        Title = @Title,
                        Description = @Description,
                        LayoutConfig = @LayoutConfig,
                        LastUpdated = @LastUpdated,
                        IsFeatured = @IsFeatured,
                        CategoryID = @CategoryID,
                        SharingStatus = @SharingStatus,
                        RefreshInterval = @RefreshInterval,
                        FiltersConfig = @FiltersConfig,
                        ValidationStatus = @ValidationStatus
                    WHERE DashboardID = @DashboardID";

                return await WithConnectionAsync(async conn =>
                {
                    using var transaction = conn.BeginTransaction();
                    try
                    {
                        // Update dashboard
                        await conn.ExecuteSafeAsync(sql, dashboard, transaction);

                        // Get existing components
                        var existingComponents = await GetDashboardComponentsAsync(dashboard.DashboardID, conn, transaction);
                        var existingComponentIds = existingComponents.Select(c => c.ComponentID).ToHashSet();

                        // Update or insert components
                        foreach (var component in dashboard.Components)
                        {
                            component.DashboardID = dashboard.DashboardID;
                            if (component.ComponentID > 0 && existingComponentIds.Contains(component.ComponentID))
                            {
                                await UpdateDashboardComponentAsync(component, conn, transaction);
                                existingComponentIds.Remove(component.ComponentID);
                            }
                            else
                            {
                                await CreateDashboardComponentAsync(component, conn, transaction);
                            }
                        }

                        // Delete removed components
                        foreach (var componentId in existingComponentIds)
                        {
                            await DeleteDashboardComponentAsync(componentId, conn, transaction);
                        }

                        transaction.Commit();
                        return true;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                });
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Deletes a dashboard.
        /// </summary>
        /// <param name="dashboardId">The ID of the dashboard to delete.</param>
        /// <returns>True if successful.</returns>
        public async Task<bool> DeleteDashboardAsync(int dashboardId)
        {
            try
            {
                const string sql = "DELETE FROM Dashboards WHERE DashboardID = @DashboardID";

                return await WithConnectionAsync(async conn =>
                {
                    using var transaction = conn.BeginTransaction();
                    try
                    {
                        // Delete components first
                        await DeleteDashboardComponentsAsync(dashboardId, conn, transaction);

                        // Delete dashboard
                        var result = await conn.ExecuteSafeAsync(sql, new { DashboardID = dashboardId }, transaction);

                        transaction.Commit();
                        return result > 0;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                });
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Gets all dashboard categories.
        /// </summary>
        /// <param name="activeOnly">Whether to return only active categories.</param>
        /// <returns>A collection of dashboard categories.</returns>
        public async Task<IEnumerable<DashboardCategory>> GetDashboardCategoriesAsync(bool activeOnly = true)
        {
            try
            {
                var whereClause = activeOnly ? "WHERE IsActive = 1" : string.Empty;
                var sql = $"SELECT * FROM DashboardCategories {whereClause} ORDER BY DisplayOrder";

                return await WithConnectionAsync(async conn =>
                {
                    return await conn.QuerySafeAsync<DashboardCategory>(sql);
                });
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Creates a new dashboard category.
        /// </summary>
        /// <param name="category">The category to create.</param>
        /// <returns>The ID of the created category.</returns>
        public async Task<int> CreateDashboardCategoryAsync(DashboardCategory category)
        {
            try
            {
                const string sql = @"
                    INSERT INTO DashboardCategories (
                        Name, Description, DisplayOrder, IconClass, CreatedAt, IsActive
                    ) VALUES (
                        @Name, @Description, @DisplayOrder, @IconClass, @CreatedAt, @IsActive
                    );
                    SELECT CAST(SCOPE_IDENTITY() as int)";

                return await WithConnectionAsync(async conn =>
                {
                    return await conn.ExecuteScalarSafeAsync<int>(sql, category);
                });
            }
            catch (Exception)
            {
                throw;
            }
        }

        #region Component Methods

        /// <summary>
        /// Gets components for a dashboard.
        /// </summary>
        /// <param name="dashboardId">The dashboard ID.</param>
        /// <param name="connection">Optional connection to use.</param>
        /// <param name="transaction">Optional transaction to use.</param>
        /// <returns>A collection of dashboard components.</returns>
        public async Task<IEnumerable<DashboardComponent>> GetDashboardComponentsAsync(
            int dashboardId, IDbConnection connection = null, IDbTransaction transaction = null)
        {
            const string sql = @"
                SELECT c.*, dt.Name as DataViewingTypeName
                FROM DashboardComponents c
                LEFT JOIN DataViewingTypes dt ON c.DataViewingTypeID = dt.DataViewingTypeID
                WHERE c.DashboardID = @DashboardID
                ORDER BY c.GridY, c.GridX";

            if (connection != null)
            {
                var components = await connection.QuerySafeAsync<DashboardComponent>(
                    sql, new { DashboardID = dashboardId }, transaction);

                foreach (var component in components)
                {
                    component.Parameters = (await GetComponentParametersAsync(
                        component.ComponentID, connection, transaction)).ToList();
                }

                return components;
            }
            else
            {
                return await WithConnectionAsync(async conn =>
                {
                    var components = await conn.QuerySafeAsync<DashboardComponent>(
                        sql, new { DashboardID = dashboardId });

                    foreach (var component in components)
                    {
                        component.Parameters = (await GetComponentParametersAsync(component.ComponentID, conn)).ToList();
                    }

                    return components;
                });
            }
        }

        /// <summary>
        /// Creates a new dashboard component.
        /// </summary>
        /// <param name="component">The component to create.</param>
        /// <param name="connection">Optional connection to use.</param>
        /// <param name="transaction">Optional transaction to use.</param>
        /// <returns>The ID of the created component.</returns>
        public async Task<int> CreateDashboardComponentAsync(
            DashboardComponent component, IDbConnection connection = null, IDbTransaction transaction = null)
        {
            const string sql = @"
                INSERT INTO DashboardComponents (
                    DashboardID, Title, Description, DataViewingTypeID, GridX,
                    GridY, GridWidth, GridHeight, QueryText, QueryIntent,
                    VisualizationConfig, IsValidated, ValidatedBy, ValidatedAt,
                    IsVisible, IsAIGenerated, RefreshInterval, FilterExpression,
                    CreatedAt, LastUpdated
                ) VALUES (
                    @DashboardID, @Title, @Description, @DataViewingTypeID, @GridX,
                    @GridY, @GridWidth, @GridHeight, @QueryText, @QueryIntent,
                    @VisualizationConfig, @IsValidated, @ValidatedBy, @ValidatedAt,
                    @IsVisible, @IsAIGenerated, @RefreshInterval, @FilterExpression,
                    @CreatedAt, @LastUpdated
                );
                SELECT CAST(SCOPE_IDENTITY() as int)";

            component.CreatedAt = DateTime.UtcNow;
            component.LastUpdated = DateTime.UtcNow;

            if (connection != null)
            {
                var componentId = await connection.ExecuteScalarSafeAsync<int>(sql, component, transaction);

                // Insert parameters if any
                if (component.Parameters?.Any() == true)
                {
                    foreach (var parameter in component.Parameters)
                    {
                        parameter.ComponentID = componentId;
                        await CreateComponentParameterAsync(parameter, connection, transaction);
                    }
                }

                return componentId;
            }
            else
            {
                return await WithConnectionAsync(async conn =>
                {
                    using var trans = conn.BeginTransaction();
                    try
                    {
                        var componentId = await conn.ExecuteScalarSafeAsync<int>(sql, component, trans);

                        // Insert parameters if any
                        if (component.Parameters?.Any() == true)
                        {
                            foreach (var parameter in component.Parameters)
                            {
                                parameter.ComponentID = componentId;
                                await CreateComponentParameterAsync(parameter, conn, trans);
                            }
                        }

                        trans.Commit();
                        return componentId;
                    }
                    catch
                    {
                        trans.Rollback();
                        throw;
                    }
                });
            }
        }

        /// <summary>
        /// Updates an existing dashboard component.
        /// </summary>
        /// <param name="component">The component to update.</param>
        /// <param name="connection">Optional connection to use.</param>
        /// <param name="transaction">Optional transaction to use.</param>
        /// <returns>True if successful.</returns>
        public async Task<bool> UpdateDashboardComponentAsync(
            DashboardComponent component, IDbConnection connection = null, IDbTransaction transaction = null)
        {
            const string sql = @"
                UPDATE DashboardComponents SET
                    Title = @Title,
                    Description = @Description,
                    DataViewingTypeID = @DataViewingTypeID,
                    GridX = @GridX,
                    GridY = @GridY,
                    GridWidth = @GridWidth,
                    GridHeight = @GridHeight,
                    QueryText = @QueryText,
                    QueryIntent = @QueryIntent,
                    VisualizationConfig = @VisualizationConfig,
                    IsValidated = @IsValidated,
                    ValidatedBy = @ValidatedBy,
                    ValidatedAt = @ValidatedAt,
                    IsVisible = @IsVisible,
                    RefreshInterval = @RefreshInterval,
                    FilterExpression = @FilterExpression,
                    LastUpdated = @LastUpdated
                WHERE ComponentID = @ComponentID";

            component.LastUpdated = DateTime.UtcNow;

            if (connection != null)
            {
                var result = await connection.ExecuteSafeAsync(sql, component, transaction);

                // Get existing parameters
                var existingParameters = await GetComponentParametersAsync(component.ComponentID, connection, transaction);
                var existingParameterIds = existingParameters.Select(p => p.ParameterID).ToHashSet();

                // Update or insert parameters
                foreach (var parameter in component.Parameters)
                {
                    parameter.ComponentID = component.ComponentID;
                    if (parameter.ParameterID > 0 && existingParameterIds.Contains(parameter.ParameterID))
                    {
                        await UpdateComponentParameterAsync(parameter, connection, transaction);
                        existingParameterIds.Remove(parameter.ParameterID);
                    }
                    else
                    {
                        await CreateComponentParameterAsync(parameter, connection, transaction);
                    }
                }

                // Delete removed parameters
                foreach (var parameterId in existingParameterIds)
                {
                    await DeleteComponentParameterAsync(parameterId, connection, transaction);
                }

                return result > 0;
            }
            else
            {
                return await WithConnectionAsync(async conn =>
                {
                    using var trans = conn.BeginTransaction();
                    try
                    {
                        var result = await conn.ExecuteSafeAsync(sql, component, trans);

                        // Get existing parameters
                        var existingParameters = await GetComponentParametersAsync(component.ComponentID, conn, trans);
                        var existingParameterIds = existingParameters.Select(p => p.ParameterID).ToHashSet();

                        // Update or insert parameters
                        foreach (var parameter in component.Parameters)
                        {
                            parameter.ComponentID = component.ComponentID;
                            if (parameter.ParameterID > 0 && existingParameterIds.Contains(parameter.ParameterID))
                            {
                                await UpdateComponentParameterAsync(parameter, conn, trans);
                                existingParameterIds.Remove(parameter.ParameterID);
                            }
                            else
                            {
                                await CreateComponentParameterAsync(parameter, conn, trans);
                            }
                        }

                        // Delete removed parameters
                        foreach (var parameterId in existingParameterIds)
                        {
                            await DeleteComponentParameterAsync(parameterId, conn, trans);
                        }

                        trans.Commit();
                        return result > 0;
                    }
                    catch
                    {
                        trans.Rollback();
                        throw;
                    }
                });
            }
        }

        /// <summary>
        /// Deletes a dashboard component.
        /// </summary>
        /// <param name="componentId">The ID of the component to delete.</param>
        /// <param name="connection">Optional connection to use.</param>
        /// <param name="transaction">Optional transaction to use.</param>
        /// <returns>True if successful.</returns>
        public async Task<bool> DeleteDashboardComponentAsync(
            int componentId, IDbConnection connection = null, IDbTransaction transaction = null)
        {
            const string sql = "DELETE FROM DashboardComponents WHERE ComponentID = @ComponentID";

            if (connection != null)
            {
                // Delete parameters first
                await DeleteComponentParametersAsync(componentId, connection, transaction);

                // Delete component
                var result = await connection.ExecuteSafeAsync(sql, new { ComponentID = componentId }, transaction);
                return result > 0;
            }
            else
            {
                return await WithConnectionAsync(async conn =>
                {
                    using var trans = conn.BeginTransaction();
                    try
                    {
                        // Delete parameters first
                        await DeleteComponentParametersAsync(componentId, conn, trans);

                        // Delete component
                        var result = await conn.ExecuteSafeAsync(sql, new { ComponentID = componentId }, trans);

                        trans.Commit();
                        return result > 0;
                    }
                    catch
                    {
                        trans.Rollback();
                        throw;
                    }
                });
            }
        }

        /// <summary>
        /// Deletes all components for a dashboard.
        /// </summary>
        /// <param name="dashboardId">The dashboard ID.</param>
        /// <param name="connection">Optional connection to use.</param>
        /// <param name="transaction">Optional transaction to use.</param>
        /// <returns>True if successful.</returns>
        public async Task<bool> DeleteDashboardComponentsAsync(
            int dashboardId, IDbConnection connection = null, IDbTransaction transaction = null)
        {
            if (connection != null)
            {
                // Get components to delete
                var components = await GetDashboardComponentsAsync(dashboardId, connection, transaction);

                // Delete each component
                foreach (var component in components)
                {
                    await DeleteDashboardComponentAsync(component.ComponentID, connection, transaction);
                }

                return true;
            }
            else
            {
                return await WithConnectionAsync(async conn =>
                {
                    using var trans = conn.BeginTransaction();
                    try
                    {
                        // Get components to delete
                        var components = await GetDashboardComponentsAsync(dashboardId, conn, trans);

                        // Delete each component
                        foreach (var component in components)
                        {
                            await DeleteDashboardComponentAsync(component.ComponentID, conn, trans);
                        }

                        trans.Commit();
                        return true;
                    }
                    catch
                    {
                        trans.Rollback();
                        throw;
                    }
                });
            }
        }

        #endregion

        #region Parameter Methods

        /// <summary>
        /// Gets parameters for a component.
        /// </summary>
        /// <param name="componentId">The component ID.</param>
        /// <param name="connection">Optional connection to use.</param>
        /// <param name="transaction">Optional transaction to use.</param>
        /// <returns>A collection of component parameters.</returns>
        public async Task<IEnumerable<ComponentParameter>> GetComponentParametersAsync(
            int componentId, IDbConnection connection = null, IDbTransaction transaction = null)
        {
            const string sql = @"
                SELECT * FROM ComponentParameters
                WHERE ComponentID = @ComponentID
                ORDER BY ParameterID";

            if (connection != null)
            {
                return await connection.QuerySafeAsync<ComponentParameter>(
                    sql, new { ComponentID = componentId }, transaction);
            }
            else
            {
                return await WithConnectionAsync(async conn =>
                {
                    return await conn.QuerySafeAsync<ComponentParameter>(
                        sql, new { ComponentID = componentId });
                });
            }
        }

        /// <summary>
        /// Creates a new component parameter.
        /// </summary>
        /// <param name="parameter">The parameter to create.</param>
        /// <param name="connection">Optional connection to use.</param>
        /// <param name="transaction">Optional transaction to use.</param>
        /// <returns>The ID of the created parameter.</returns>
        public async Task<int> CreateComponentParameterAsync(
            ComponentParameter parameter, IDbConnection connection = null, IDbTransaction transaction = null)
        {
            const string sql = @"
                INSERT INTO ComponentParameters (
                    ComponentID, Name, DisplayName, DefaultValue, CurrentValue,
                    DataType, IsRequired, Options, IsVisible, ValidationRules, Description
                ) VALUES (
                    @ComponentID, @Name, @DisplayName, @DefaultValue, @CurrentValue,
                    @DataType, @IsRequired, @Options, @IsVisible, @ValidationRules, @Description
                );
                SELECT CAST(SCOPE_IDENTITY() as int)";

            if (connection != null)
            {
                return await connection.ExecuteScalarSafeAsync<int>(sql, parameter, transaction);
            }
            else
            {
                return await WithConnectionAsync(async conn =>
                {
                    return await conn.ExecuteScalarSafeAsync<int>(sql, parameter);
                });
            }
        }

        /// <summary>
        /// Updates an existing component parameter.
        /// </summary>
        /// <param name="parameter">The parameter to update.</param>
        /// <param name="connection">Optional connection to use.</param>
        /// <param name="transaction">Optional transaction to use.</param>
        /// <returns>True if successful.</returns>
        public async Task<bool> UpdateComponentParameterAsync(
            ComponentParameter parameter, IDbConnection connection = null, IDbTransaction transaction = null)
        {
            const string sql = @"
                UPDATE ComponentParameters SET
                    Name = @Name,
                    DisplayName = @DisplayName,
                    DefaultValue = @DefaultValue,
                    CurrentValue = @CurrentValue,
                    DataType = @DataType,
                    IsRequired = @IsRequired,
                    Options = @Options,
                    IsVisible = @IsVisible,
                    ValidationRules = @ValidationRules,
                    Description = @Description
                WHERE ParameterID = @ParameterID";

            if (connection != null)
            {
                var result = await connection.ExecuteSafeAsync(sql, parameter, transaction);
                return result > 0;
            }
            else
            {
                return await WithConnectionAsync(async conn =>
                {
                    var result = await conn.ExecuteSafeAsync(sql, parameter);
                    return result > 0;
                });
            }
        }

        /// <summary>
        /// Deletes a component parameter.
        /// </summary>
        /// <param name="parameterId">The ID of the parameter to delete.</param>
        /// <param name="connection">Optional connection to use.</param>
        /// <param name="transaction">Optional transaction to use.</param>
        /// <returns>True if successful.</returns>
        public async Task<bool> DeleteComponentParameterAsync(
            int parameterId, IDbConnection connection = null, IDbTransaction transaction = null)
        {
            const string sql = "DELETE FROM ComponentParameters WHERE ParameterID = @ParameterID";

            if (connection != null)
            {
                var result = await connection.ExecuteSafeAsync(sql, new { ParameterID = parameterId }, transaction);
                return result > 0;
            }
            else
            {
                return await WithConnectionAsync(async conn =>
                {
                    var result = await conn.ExecuteSafeAsync(sql, new { ParameterID = parameterId });
                    return result > 0;
                });
            }
        }

        /// <summary>
        /// Deletes all parameters for a component.
        /// </summary>
        /// <param name="componentId">The component ID.</param>
        /// <param name="connection">Optional connection to use.</param>
        /// <param name="transaction">Optional transaction to use.</param>
        /// <returns>True if successful.</returns>
        public async Task<bool> DeleteComponentParametersAsync(
            int componentId, IDbConnection connection = null, IDbTransaction transaction = null)
        {
            const string sql = "DELETE FROM ComponentParameters WHERE ComponentID = @ComponentID";

            if (connection != null)
            {
                var result = await connection.ExecuteSafeAsync(sql, new { ComponentID = componentId }, transaction);
                return true;
            }
            else
            {
                return await WithConnectionAsync(async conn =>
                {
                    var result = await conn.ExecuteSafeAsync(sql, new { ComponentID = componentId });
                    return true;
                });
            }
        }

        #endregion
    }
}