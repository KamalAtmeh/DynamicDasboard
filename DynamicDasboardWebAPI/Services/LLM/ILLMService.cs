using DynamicDashboardCommon.Models.LLM;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DynamicDasboardWebAPI.Services.LLM
{
    /// <summary>
    /// Interface for Language Model services that support natural language processing capabilities.
    /// This abstraction allows switching between different LLM providers (DeepSeek, Claude, etc.)
    /// </summary>
    public interface ILLMService
    {
        /// <summary>
        /// Analyzes a natural language question and generates an explanation of its understanding.
        /// </summary>
        /// <param name="question">The natural language question</param>
        /// <param name="databaseSchema">The database schema with tables, columns, and relationships</param>
        /// <param name="adminDescriptions">Optional dictionary of admin descriptions for database entities</param>
        /// <returns>An explanation response including understanding explanation and identified ambiguities</returns>
        Task<ExplanationResponse> GenerateExplanationAsync(
            string question,
            string databaseSchema,
            Dictionary<string, string> adminDescriptions = null);

        /// <summary>
        /// Generates SQL from a natural language question after confirmation of understanding.
        /// </summary>
        /// <param name="question">The original natural language question</param>
        /// <param name="confirmedUnderstanding">The confirmed explanation of the question's meaning</param>
        /// <param name="databaseSchema">The database schema with tables, columns, and relationships</param>
        /// <param name="resolvedAmbiguities">Dictionary of ambiguities and their resolutions</param>
        /// <returns>A generated SQL query</returns>
        Task<string> GenerateSqlAsync(
            string question,
            string confirmedUnderstanding,
            string databaseSchema,
            Dictionary<string, string> resolvedAmbiguities = null);

        /// <summary>
        /// Generates an explanation for SQL query results.
        /// </summary>
        /// <param name="question">The original natural language question</param>
        /// <param name="sql">The SQL query that was executed</param>
        /// <param name="results">The query results</param>
        /// <returns>A user-friendly explanation of the results</returns>
        Task<string> GenerateResultExplanationAsync(
            string question,
            string sql,
            List<Dictionary<string, object>> results);
    }
}