using DynamicDashboardCommon.Enums;

using DynamicDasboardWebAPI.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DynamicDashboardCommon.Models;

namespace DynamicDasboardWebAPI.Controllers
{
    /// <summary>
    /// API controller for dashboard operations.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : AppControllerBase
    {
        private readonly IDashboardService _dashboardService;

        /// <summary>
        /// Initializes a new instance of the <see cref="DashboardController"/> class.
        /// </summary>
        /// <param name="dashboardService">The dashboard service.</param>
        /// <param name="logsService">The logs service.</param>
        public DashboardController(
            IDashboardService dashboardService,
            ILogsService logsService)
            : base(logsService)
        {
            _dashboardService = dashboardService ?? throw new ArgumentNullException(nameof(dashboardService));
        }

        /// <summary>
        /// Gets all dashboards with optional filtering.
        /// </summary>
        /// <param name="categoryId">Optional category ID filter.</param>
        /// <param name="createdBy">Optional creator ID filter.</param>
        /// <param name="sharingStatus">Optional sharing status filter (1=Private, 2=Shared, 3=Public).</param>
        /// <returns>A collection of dashboards.</returns>
        [HttpGet]
        public async Task<IActionResult> GetAllDashboards(
            [FromQuery] int? categoryId = null,
            [FromQuery] int? createdBy = null,
            [FromQuery] int? sharingStatus = null)
        {
            try
            {
                DashboardSharingStatus? sharingStatusEnum = null;
                if (sharingStatus.HasValue && Enum.IsDefined(typeof(DashboardSharingStatus), sharingStatus.Value))
                {
                    sharingStatusEnum = (DashboardSharingStatus)sharingStatus.Value;
                }

                var dashboards = await _dashboardService.GetAllDashboardsAsync(categoryId, createdBy, sharingStatusEnum);
                return Ok(dashboards);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, EnumLoggingType.Error.ToString());
            }
        }

        /// <summary>
        /// Gets a dashboard by its ID.
        /// </summary>
        /// <param name="id">The dashboard ID.</param>
        /// <returns>The dashboard.</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetDashboardById(int id)
        {
            try
            {
                var dashboard = await _dashboardService.GetDashboardByIdAsync(id);
                if (dashboard == null)
                {
                    return NotFound($"Dashboard with ID {id} not found.");
                }

                return Ok(dashboard);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, EnumLoggingType.Error.ToString());
            }
        }

        /// <summary>
        /// Creates a new dashboard.
        /// </summary>
        /// <param name="dashboard">The dashboard to create.</param>
        /// <returns>The created dashboard.</returns>
        [HttpPost]
        public async Task<IActionResult> CreateDashboard([FromBody] DynamicDashboardCommon.Models.DashboardModel dashboard)
        {
            try
            {
                var dashboardId = await _dashboardService.CreateDashboardAsync(dashboard);
                var createdDashboard = await _dashboardService.GetDashboardByIdAsync(dashboardId);
                return CreatedAtAction(nameof(GetDashboardById), new { id = dashboardId }, createdDashboard);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, EnumLoggingType.Error.ToString());
            }
        }

        /// <summary>
        /// Updates an existing dashboard.
        /// </summary>
        /// <param name="id">The ID of the dashboard to update.</param>
        /// <param name="dashboard">The updated dashboard data.</param>
        /// <returns>The updated dashboard.</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDashboard(int id, [FromBody] DynamicDashboardCommon.Models.DashboardModel dashboard)
        {
            try
            {
                if (id != dashboard.DashboardID)
                {
                    return BadRequest("Dashboard ID mismatch.");
                }

                var success = await _dashboardService.UpdateDashboardAsync(dashboard);
                if (!success)
                {
                    return NotFound($"Dashboard with ID {id} not found.");
                }

                var updatedDashboard = await _dashboardService.GetDashboardByIdAsync(id);
                return Ok(updatedDashboard);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, EnumLoggingType.Error.ToString());
            }
        }

        /// <summary>
        /// Deletes a dashboard.
        /// </summary>
        /// <param name="id">The ID of the dashboard to delete.</param>
        /// <returns>Success indicator.</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDashboard(int id)
        {
            try
            {
                var success = await _dashboardService.DeleteDashboardAsync(id);
                if (!success)
                {
                    return NotFound($"Dashboard with ID {id} not found.");
                }

                return Ok(new { Success = true, Message = $"Dashboard with ID {id} deleted successfully." });
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, EnumLoggingType.Error.ToString());
            }
        }

        /// <summary>
        /// Gets all dashboard categories.
        /// </summary>
        /// <param name="activeOnly">Whether to return only active categories (default: true).</param>
        /// <returns>A collection of dashboard categories.</returns>
        [HttpGet("categories")]
        public async Task<IActionResult> GetDashboardCategories([FromQuery] bool activeOnly = true)
        {
            try
            {
                var categories = await _dashboardService.GetDashboardCategoriesAsync(activeOnly);
                return Ok(categories);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, EnumLoggingType.Error.ToString());
            }
        }

        /// <summary>
        /// Creates a new dashboard category.
        /// </summary>
        /// <param name="category">The category to create.</param>
        /// <returns>The created category.</returns>
        [HttpPost("categories")]
        public async Task<IActionResult> CreateDashboardCategory([FromBody] DashboardCategory category)
        {
            try
            {
                var categoryId = await _dashboardService.CreateDashboardCategoryAsync(category);
                var categories = await _dashboardService.GetDashboardCategoriesAsync();
                var createdCategory = categories.FirstOrDefault(c => c.CategoryID == categoryId);

                return CreatedAtAction(nameof(GetDashboardCategories), createdCategory);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, EnumLoggingType.Error.ToString());
            }
        }

        /// <summary>
        /// Generates AI dashboard suggestions based on database schema.
        /// </summary>
        /// <param name="databaseId">The database ID to analyze.</param>
        /// <returns>A list of suggested dashboards.</returns>
        [HttpGet("suggestions/{databaseId}")]
        public async Task<IActionResult> GenerateDashboardSuggestions(int databaseId)
        {
            try
            {
                var suggestions = await _dashboardService.GenerateDashboardSuggestionsAsync(databaseId);
                return Ok(suggestions);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, EnumLoggingType.Error.ToString());
            }
        }
    }
}