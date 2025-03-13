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
        public Dictionary<string, QueryParameterOptions> AdjustableParameters { get; set; } = new Dictionary<string, QueryParameterOptions>();

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

        /// <summary>
        /// Flag indicating whether the question is related to the database schema
        /// </summary>
        public bool IsSchemaRelated { get; set; } = true;

        /// <summary>
        /// Message explaining why the question is not related to the schema (if applicable)
        /// </summary>
        public string SchemaRelevanceMessage { get; set; }

        /// <summary>
        /// List of suggested topics the user can ask about based on the schema
        /// </summary>
        public List<string> SuggestedTopics { get; set; } = new List<string>();

        /// <summary>
        /// List of suggested questions related to the schema
        /// </summary>
        public List<string> SuggestedQuestions { get; set; } = new List<string>();

        /// <summary>
        /// Parts of the question that are not related to the schema
        /// </summary>
        public List<string> UnrelatedQuestionParts { get; set; } = new List<string>();

        /// <summary>
        /// Flag indicating if part of the question (but not all) is unrelated to the schema
        /// </summary>
        public bool HasPartiallyUnrelatedContent { get; set; }
    }
}