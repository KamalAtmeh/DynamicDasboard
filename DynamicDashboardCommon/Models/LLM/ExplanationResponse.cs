using System.Collections.Generic;

namespace DynamicDashboardCommon.Models.LLM
{
    /// <summary>
    /// Represents a response from the LLM containing an explanation of how it understood a natural language query,
    /// along with any detected ambiguities or adjustable parameters.
    /// </summary>
    public class ExplanationResponse
    {
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
        /// System's confidence in its understanding (0.0 to 1.0)
        /// </summary>
        public double ConfidenceScore { get; set; }

        /// <summary>
        /// The SQL query that would be generated (preview only, not for execution)
        /// </summary>
        public string PreviewSql { get; set; }

        /// <summary>
        /// Dictionary mapping technical terms to admin-friendly terms for reference
        /// </summary>
        public Dictionary<string, string> TermMapping { get; set; } = new Dictionary<string, string>();
    }


}