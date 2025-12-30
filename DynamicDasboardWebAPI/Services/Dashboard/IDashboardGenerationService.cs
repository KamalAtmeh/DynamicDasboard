using DynamicDashboardCommon.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DynamicDasboardWebAPI.Services
{
    /// <summary>
    /// Interface for dashboard generation services with template support.
    /// </summary>
    public interface IDashboardGenerationService
    {
        /// <summary>
        /// Generates dashboard suggestions based on database schema using default template.
        /// </summary>
        /// <param name="databaseId">The database ID to analyze.</param>
        /// <returns>A list of suggested dashboards.</returns>
        Task<List<DashboardModel>> GenerateDashboardSuggestionsAsync(int databaseId);

        /// <summary>
        /// Generates dashboard suggestions based on database schema using a specific template.
        /// </summary>
        /// <param name="databaseId">The database ID to analyze.</param>
        /// <param name="templateId">The template ID to use (e.g., "executive-standard", "operational-dashboard").</param>
        /// <returns>A list of suggested dashboards.</returns>
        Task<List<DashboardModel>> GenerateDashboardSuggestionsAsync(int databaseId, string templateId);
    }
}
