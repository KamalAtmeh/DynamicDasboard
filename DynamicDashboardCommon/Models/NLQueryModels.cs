using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicDashboardCommon.Models
{
    using global::DynamicDashboardCommon.Models.LLM;
    using System.Collections.Generic;

    namespace DynamicDashboardCommon.Models
    {
        /// <summary>
        /// Response from analyzing a natural language question
        /// </summary>
        public class AnalysisResponse
        {
            /// <summary>
            /// The original question
            /// </summary>
            public string Question { get; set; }

            /// <summary>
            /// The database ID
            /// </summary>
            public int DatabaseId { get; set; }

            /// <summary>
            /// A user-friendly explanation of how the system understood the question
            /// </summary>
            public string Explanation { get; set; }

            /// <summary>
            /// Flag indicating whether the system detected any ambiguities
            /// </summary>
            public bool HasAmbiguities { get; set; }

            /// <summary>
            /// Dictionary of detected ambiguities with possible interpretations
            /// Key = ambiguous term, Value = list of possible interpretations
            /// </summary>
            public Dictionary<string, List<string>> DetectedAmbiguities { get; set; } = new Dictionary<string, List<string>>();

            /// <summary>
            /// Dictionary of adjustable parameters with default values and possible alternatives
            /// Key = parameter name, Value = parameter details
            /// </summary>
            public Dictionary<string, ParameterOptions> AdjustableParameters { get; set; } = new Dictionary<string, ParameterOptions>();

            /// <summary>
            /// Preview of the SQL that would be generated (for reference only)
            /// </summary>
            public string PreviewSql { get; set; }

            /// <summary>
            /// System's confidence in its understanding (0.0 to 1.0)
            /// </summary>
            public double ConfidenceScore { get; set; }

            /// <summary>
            /// Flag indicating whether the analysis was successful
            /// </summary>
            public bool Success { get; set; }

            /// <summary>
            /// Error message if analysis failed
            /// </summary>
            public string ErrorMessage { get; set; }
        }

        /// <summary>
        /// Request to confirm understanding and generate SQL
        /// </summary>
        public class NlQueryConfirmationRequest
        {
            /// <summary>
            /// The original natural language question
            /// </summary>
            public string OriginalQuestion { get; set; }

            /// <summary>
            /// The database ID
            /// </summary>
            public int DatabaseId { get; set; }

            /// <summary>
            /// The confirmed explanation of understanding
            /// </summary>
            public string ConfirmedUnderstanding { get; set; }

            /// <summary>
            /// Dictionary of resolved ambiguities
            /// Key = ambiguous term, Value = chosen interpretation
            /// </summary>
            public Dictionary<string, string> ResolvedAmbiguities { get; set; } = new Dictionary<string, string>();

            /// <summary>
            /// Dictionary of adjusted parameters
            /// Key = parameter name, Value = chosen value
            /// </summary>
            public Dictionary<string, string> AdjustedParameters { get; set; } = new Dictionary<string, string>();
        }

        /// <summary>
        /// Response from SQL generation
        /// </summary>
        public class SqlGenerationResponse
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
            /// Flag indicating whether SQL generation was successful
            /// </summary>
            public bool Success { get; set; }

            /// <summary>
            /// Error message if SQL generation failed
            /// </summary>
            public string ErrorMessage { get; set; }
        }

        /// <summary>
        /// Request to execute a SQL query
        /// </summary>
        public class SqlExecutionRequest
        {
            /// <summary>
            /// The original natural language question (optional)
            /// </summary>
            public string OriginalQuestion { get; set; }

            /// <summary>
            /// The database ID
            /// </summary>
            public int DatabaseId { get; set; }

            /// <summary>
            /// The SQL query to execute
            /// </summary>
            public string Sql { get; set; }
        }

        /// <summary>
        /// Response from query execution
        /// </summary>
        public class QueryExecutionResponse
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
            /// The executed SQL query
            /// </summary>
            public string Sql { get; set; }

            /// <summary>
            /// The query results
            /// </summary>
            public List<Dictionary<string, object>> Results { get; set; } = new List<Dictionary<string, object>>();

            /// <summary>
            /// A user-friendly explanation of the results
            /// </summary>
            public string ResultExplanation { get; set; }

            /// <summary>
            /// The recommended data viewing type ID
            /// </summary>
            public int? RecommendedDataViewingTypeID { get; set; }

            /// <summary>
            /// The recommended data viewing type name
            /// </summary>
            public string RecommendedDataViewingTypeName { get; set; }

            /// <summary>
            /// Formatted result for label-type viewing
            /// </summary>
            public string FormattedResult { get; set; }

            /// <summary>
            /// Flag indicating whether query execution was successful
            /// </summary>
            public bool Success { get; set; }

            /// <summary>
            /// Error message if query execution failed
            /// </summary>
            public string ErrorMessage { get; set; }
        }
    }
}
