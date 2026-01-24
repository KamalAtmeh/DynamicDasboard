using DynamicDashboardCommon.Models;

namespace DynamicDasboardWebAPI.Services
{
    /// <summary>
    /// Interface for AI-powered dashboard generation service.
    /// Uses templates for layout and LLM for intelligent content generation.
    /// </summary>
    public interface IDashboardGenerationService
    {
        /// <summary>
        /// Generates dashboard suggestions using default template.
        /// </summary>
        /// <param name="databaseId">Target database ID</param>
        /// <returns>List of generated dashboard models</returns>
        Task<List<DashboardModel>> GenerateDashboardSuggestionsAsync(int databaseId);

        /// <summary>
        /// Generates a dashboard using template layout and LLM-generated content.
        /// Template defines positions (grid layout), LLM generates content (titles, queries, chart types).
        /// </summary>
        /// <param name="databaseId">Target database ID to analyze schema from</param>
        /// <param name="templateId">Template ID defining layout and AI guidance</param>
        /// <returns>List containing generated dashboard with intelligent components</returns>
        Task<List<DashboardModel>> GenerateDashboardSuggestionsAsync(int databaseId, string templateId);

        /// <summary>
        /// Generates suggested questions based on database schema
        /// </summary>
        Task<List<string>> GenerateSuggestedQuestionsAsync(int databaseId);
    }
}