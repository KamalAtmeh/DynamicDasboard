
using DynamicDasboardWebAPI.Repositories;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using DynamicDashboardCommon.Models;
using System.Text.Json;
using DynamicDashboardCommon.Enums;

namespace DynamicDasboardWebAPI.Services
{
    /// <summary>
    /// Service for managing dashboard operations.
    /// </summary>
    public class DashboardService : IDashboardService
    {
        private readonly DashboardRepository _dashboardRepository;
        private readonly ILogsService _logsService;

        /// <summary>
        /// Initializes a new instance of the <see cref="DashboardService"/> class.
        /// </summary>
        /// <param name="dashboardRepository">The dashboard repository.</param>
        /// <param name="logsService">The logs service.</param>
        public DashboardService(
            DashboardRepository dashboardRepository,
            ILogsService logsService)
        {
            _dashboardRepository = dashboardRepository ?? throw new ArgumentNullException(nameof(dashboardRepository));
            _logsService = logsService ?? throw new ArgumentNullException(nameof(logsService));
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
                return await _dashboardRepository.GetAllDashboardsAsync(categoryId, createdBy, sharingStatus);
            }
            catch (Exception ex)
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
                return await _dashboardRepository.GetDashboardByIdAsync(dashboardId);
            }
            catch (Exception ex)
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
                ValidateDashboard(dashboard);

                // Set default values for new dashboard
                dashboard.CreatedAt = DateTime.UtcNow;
                dashboard.LastUpdated = DateTime.UtcNow;

                return await _dashboardRepository.CreateDashboardAsync(dashboard);
            }
            catch (Exception ex)
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
                ValidateDashboard(dashboard);

                // Make sure dashboard exists
                var existingDashboard = await _dashboardRepository.GetDashboardByIdAsync(dashboard.DashboardID);
                if (existingDashboard == null)
                {
                    throw new ArgumentException($"Dashboard with ID {dashboard.DashboardID} not found.");
                }

                // Update timestamp
                dashboard.LastUpdated = DateTime.UtcNow;

                return await _dashboardRepository.UpdateDashboardAsync(dashboard);
            }
            catch (Exception ex)
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
                return await _dashboardRepository.DeleteDashboardAsync(dashboardId);
            }
            catch (Exception ex)
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
                return await _dashboardRepository.GetDashboardCategoriesAsync(activeOnly);
            }
            catch (Exception ex)
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
                // Validate category
                if (string.IsNullOrWhiteSpace(category.Name))
                {
                    throw new ArgumentException("Category name is required.");
                }

                // Set default values
                category.CreatedAt = DateTime.UtcNow;

                return await _dashboardRepository.CreateDashboardCategoryAsync(category);
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        /// <summary>
        /// Generates AI dashboard suggestions based on database schema.
        /// </summary>
        /// <param name="databaseId">The database ID to analyze.</param>
        /// <returns>A list of suggested dashboards.</returns>
        public async Task<List<DashboardModel>> GenerateDashboardSuggestionsAsync(int databaseId)
        {
            try
            {
                // This will be implemented with LLM integration in a separate file
                // For now, return an empty list
                return new List<DashboardModel>();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        /// <summary>
        /// Validates a dashboard.
        /// </summary>
        /// <param name="dashboard">The dashboard to validate.</param>
        private void ValidateDashboard(DashboardModel dashboard)
        {
            if (dashboard == null)
            {
                throw new ArgumentNullException(nameof(dashboard));
            }

            if (string.IsNullOrWhiteSpace(dashboard.Title))
            {
                throw new ArgumentException("Dashboard title is required.");
            }

            if (dashboard.DatabaseID <= 0)
            {
                throw new ArgumentException("Valid database ID is required.");
            }

            if (dashboard.CategoryID <= 0)
            {
                throw new ArgumentException("Valid category ID is required.");
            }

            // Validate components if any
            if (dashboard.Components?.Any() == true)
            {
                foreach (var component in dashboard.Components)
                {
                    ValidateComponent(component);
                }
            }
        }


        /// <summary>
        /// Validates a dashboard component.
        /// </summary>
        /// <param name="component">The component to validate.</param>
        private void ValidateComponent(DashboardComponent component)
        {
            if (component == null)
            {
                throw new ArgumentNullException(nameof(component));
            }

            if (string.IsNullOrWhiteSpace(component.Title))
            {
                throw new ArgumentException("Component title is required.");
            }

            if (component.DataViewingTypeID <= 0)
            {
                throw new ArgumentException("Valid data viewing type ID is required.");
            }

            // If the component uses a SQL query, it must be provided
            if (component.DataViewingTypeID != (int)DataViewingTypeEnum.Label &&
                string.IsNullOrWhiteSpace(component.QueryText))
            {
                throw new ArgumentException($"SQL query is required for component '{component.Title}'.");
            }

            // Validate grid placement
            if (component.GridX < 0 || component.GridY < 0 ||
                component.GridWidth <= 0 || component.GridHeight <= 0)
            {
                throw new ArgumentException($"Invalid grid placement for component '{component.Title}'.");
            }

            // Validate parameters if any
            if (component.Parameters?.Any() == true)
            {
                foreach (var parameter in component.Parameters)
                {
                    ValidateParameter(parameter);
                }
            }
        }

        /// <summary>
        /// Validates a component parameter.
        /// </summary>
        /// <param name="parameter">The parameter to validate.</param>
        private void ValidateParameter(ComponentParameter parameter)
        {
            if (parameter == null)
            {
                throw new ArgumentNullException(nameof(parameter));
            }

            if (string.IsNullOrWhiteSpace(parameter.Name))
            {
                throw new ArgumentException("Parameter name is required.");
            }

            if (string.IsNullOrWhiteSpace(parameter.DisplayName))
            {
                parameter.DisplayName = parameter.Name;
            }

            // If parameter is required, it must have a default value
            if (parameter.IsRequired && string.IsNullOrWhiteSpace(parameter.DefaultValue))
            {
                throw new ArgumentException($"Default value is required for parameter '{parameter.Name}'.");
            }
        }
    }
}