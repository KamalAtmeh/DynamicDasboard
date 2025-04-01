using DynamicDashboardCommon.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DynamicDasboardWebAPI.Services
{
    /// <summary>
    /// Interface for dashboard services.
    /// </summary>
    public interface IDashboardService
    {
        /// <summary>
        /// Gets all dashboards with optional filtering.
        /// </summary>
        /// <param name="categoryId">Optional category filter.</param>
        /// <param name="createdBy">Optional creator filter.</param>
        /// <param name="sharingStatus">Optional sharing status filter.</param>
        /// <returns>A collection of dashboards.</returns>
        Task<IEnumerable<DashboardModel>> GetAllDashboardsAsync(
            int? categoryId = null, int? createdBy = null, DashboardSharingStatus? sharingStatus = null);

        /// <summary>
        /// Gets a dashboard by its ID.
        /// </summary>
        /// <param name="dashboardId">The dashboard ID.</param>
        /// <returns>The dashboard.</returns>
        Task<DashboardModel> GetDashboardByIdAsync(int dashboardId);

        /// <summary>
        /// Creates a new dashboard.
        /// </summary>
        /// <param name="dashboard">The dashboard to create.</param>
        /// <returns>The ID of the created dashboard.</returns>
        Task<int> CreateDashboardAsync(DynamicDashboardCommon.Models.DashboardModel dashboard);

        /// <summary>
        /// Updates an existing dashboard.
        /// </summary>
        /// <param name="dashboard">The dashboard to update.</param>
        /// <returns>True if successful.</returns>
        Task<bool> UpdateDashboardAsync(DynamicDashboardCommon.Models.DashboardModel dashboard);

        /// <summary>
        /// Deletes a dashboard.
        /// </summary>
        /// <param name="dashboardId">The ID of the dashboard to delete.</param>
        /// <returns>True if successful.</returns>
        Task<bool> DeleteDashboardAsync(int dashboardId);

        /// <summary>
        /// Gets all dashboard categories.
        /// </summary>
        /// <param name="activeOnly">Whether to return only active categories.</param>
        /// <returns>A collection of dashboard categories.</returns>
        Task<IEnumerable<DashboardCategory>> GetDashboardCategoriesAsync(bool activeOnly = true);

        /// <summary>
        /// Creates a new dashboard category.
        /// </summary>
        /// <param name="category">The category to create.</param>
        /// <returns>The ID of the created category.</returns>
        Task<int> CreateDashboardCategoryAsync(DashboardCategory category);

        /// <summary>
        /// Generates AI dashboard suggestions based on database schema.
        /// </summary>
        /// <param name="databaseId">The database ID to analyze.</param>
        /// <returns>A list of suggested dashboards.</returns>
        Task<List<DashboardModel>> GenerateDashboardSuggestionsAsync(int databaseId);
    }
}