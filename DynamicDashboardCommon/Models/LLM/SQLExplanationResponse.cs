using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DynamicDashboardCommon.Models.LLM
{
    /// <summary>
    /// Represents a structured response from the LLM with SQL and explanation
    /// </summary>
    public class SqlExplanationResponse
    {
        /// <summary>
        /// The generated SQL query
        /// </summary>
        [JsonPropertyName("sqlQuery")]
        public string SqlQuery { get; set; }

        /// <summary>
        /// A user-friendly explanation of what the query does
        /// </summary>
        [JsonPropertyName("businessExplanation")]
        public string BusinessExplanation { get; set; }

        /// <summary>
        /// The database type this SQL is optimized for (e.g., 'SQL Server', 'MySQL', 'Oracle')
        /// </summary>
        [JsonPropertyName("dbType")]
        public string DbType { get; set; }

        /// <summary>
        /// Notes about database compatibility or adaptation requirements
        /// </summary>
        [JsonPropertyName("dbNotes")]
        public string DbNotes { get; set; }

        /// <summary>
        /// Flag indicating whether the system detected any ambiguities
        /// </summary>
        [JsonPropertyName("hasAmbiguities")]
        public bool HasAmbiguities { get; set; }

        /// <summary>
        /// Dictionary of detected ambiguities with possible interpretations
        /// Key = ambiguous term, Value = list of possible interpretations
        /// </summary>
        [JsonPropertyName("detectedAmbiguities")]
        public Dictionary<string, List<string>> DetectedAmbiguities { get; set; } = new Dictionary<string, List<string>>();

        /// <summary>
        /// Dictionary of adjustable parameters with default values and possible alternatives
        /// </summary>
        [JsonPropertyName("adjustableParameters")]
        public Dictionary<string, AdjustableParameter> AdjustableParameters { get; set; } = new Dictionary<string, AdjustableParameter>();

        /// <summary>
        /// Dictionary mapping technical terms to friendly terms
        /// </summary>
        [JsonPropertyName("termMapping")]
        public Dictionary<string, string> TermMapping { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// Represents an adjustable parameter with default value and alternatives
    /// </summary>
    public class AdjustableParameter
    {
        /// <summary>
        /// The default value for the parameter
        /// </summary>
        [JsonPropertyName("default")]
        public object Default { get; set; }

        /// <summary>
        /// List of alternative values for the parameter
        /// </summary>
        [JsonPropertyName("alternatives")]
        public List<string> Alternatives { get; set; } = new List<string>();
    }
}