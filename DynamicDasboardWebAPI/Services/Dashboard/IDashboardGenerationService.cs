
using DynamicDashboardCommon.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DynamicDasboardWebAPI.Services
{
    /// <summary>
    /// Interface for dashboard generation services.
    /// </summary>
    public interface IDashboardGenerationService
    {
        /// <summary>
        /// Generates dashboard suggestions based on database schema.
        /// </summary>
        /// <param name="databaseId">The database ID to analyze.</param>
        /// <returns>A list of suggested dashboards.</returns>
        Task<List<DashboardModel>> GenerateDashboardSuggestionsAsync(int databaseId);
    }
}