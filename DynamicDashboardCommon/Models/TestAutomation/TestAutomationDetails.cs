using System.Collections.Generic;

namespace DynamicDashboardCommon.Models
{
    /// <summary>
    /// Represents details for a specific test case within a test automation job.
    /// </summary>
    public class TestAutomationDetail
    {
        /// <summary>
        /// The unique identifier for the test detail.
        /// </summary>
        public int DetailID { get; set; }

        /// <summary>
        /// The ID of the job this detail belongs to.
        /// </summary>
        public int JobID { get; set; }

        /// <summary>
        /// The natural language question being tested.
        /// </summary>
        public string Question { get; set; }

        /// <summary>
        /// The expected SQL query that should be generated.
        /// </summary>
        public string ExpectedSQL { get; set; }

        /// <summary>
        /// The SQL query that was actually generated.
        /// </summary>
        public string GeneratedSQL { get; set; }

        /// <summary>
        /// The similarity score between expected and generated SQL (0-1).
        /// </summary>
        public decimal? SQLMatchScore { get; set; }

        /// <summary>
        /// The expected explanation of the query.
        /// </summary>
        public string ExpectedExplanation { get; set; }

        /// <summary>
        /// The explanation that was actually generated.
        /// </summary>
        public string GeneratedExplanation { get; set; }

        /// <summary>
        /// The similarity score between expected and generated explanations (0-1).
        /// </summary>
        public decimal? ExplanationMatchScore { get; set; }

        /// <summary>
        /// The expected number of rows in the query result.
        /// </summary>
        public int? ExpectedRowCount { get; set; }

        /// <summary>
        /// The actual number of rows in the query result.
        /// </summary>
        public int? ActualRowCount { get; set; }

        /// <summary>
        /// The similarity score between expected and actual datasets (0-1).
        /// </summary>
        public decimal? DataMatchScore { get; set; }

        /// <summary>
        /// The status of the result match (e.g., "Exact Match", "Row Count Match", "Mismatch").
        /// </summary>
        public string ResultMatchStatus { get; set; }

        /// <summary>
        /// The complexity level of the query (e.g., "Simple", "Medium", "Complex").
        /// </summary>
        public string ComplexityLevel { get; set; }

        /// <summary>
        /// The category of the query (e.g., "Aggregate", "Join", "Filter").
        /// </summary>
        public string QueryCategory { get; set; }

        /// <summary>
        /// The execution time of the query in milliseconds.
        /// </summary>
        public int? ExecutionTimeMs { get; set; }

        /// <summary>
        /// Indicates whether the test was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Any error message that occurred during processing.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Navigation property for the parent job.
        /// </summary>
        public virtual TestAutomationJob Job { get; set; }

        /// <summary>
        /// Navigation property for the datasets.
        /// </summary>
        public virtual ICollection<TestAutomationDataset> Datasets { get; set; }
    }
}