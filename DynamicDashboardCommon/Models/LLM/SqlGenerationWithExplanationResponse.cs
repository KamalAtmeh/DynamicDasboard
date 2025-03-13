using System.Collections.Generic;
using DynamicDashboardCommon.Models.LLM;

namespace DynamicDashboardCommon.Models
{
    /// <summary>
    /// Response for SQL generation with explanation
    /// </summary>
    public class SqlGenerationWithExplanationResponse
    {
        /// <summary>
        /// The original question
        /// </summary>
        public string OriginalQuestion { get; set; }

        /// <summary>
        /// The database ID
        /// </summary>
        public int DatabaseId { get; set; }

        /// <summary>
        /// The generated SQL query
        /// </summary>
        public string GeneratedSql { get; set; }

        /// <summary>
        /// A user-friendly explanation of what the query does
        /// </summary>
        public string BusinessExplanation { get; set; }

        /// <summary>
        /// The database type this SQL is optimized for
        /// </summary>
        public string DbType { get; set; }

        /// <summary>
        /// Notes about database compatibility or adaptation requirements
        /// </summary>
        public string DbNotes { get; set; }

        /// <summary>
        /// Flag indicating whether the system detected any ambiguities
        /// </summary>
        public bool HasAmbiguities { get; set; }

        /// <summary>
        /// Dictionary of detected ambiguities with possible interpretations
        /// </summary>
        public Dictionary<string, List<string>> DetectedAmbiguities { get; set; } = new Dictionary<string, List<string>>();

        /// <summary>
        /// Dictionary of adjustable parameters with default values and possible alternatives
        /// </summary>
        public Dictionary<string, ParameterOptions> AdjustableParameters { get; set; } = new Dictionary<string, ParameterOptions>();

        /// <summary>
        /// Dictionary mapping technical terms to friendly terms
        /// </summary>
        public Dictionary<string, string> TermMapping { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Flag indicating whether the SQL is valid
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Validation error message if SQL is invalid
        /// </summary>
        public string ValidationErrorMessage { get; set; }

        /// <summary>
        /// Flag indicating whether SQL generation was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Error message if SQL generation failed
        /// </summary>
        public string ErrorMessage { get; set; }
    }
}