using System;

namespace DynamicDashboardCommon.Models.TestAutomation
{
    /// <summary>
    /// Lightweight metadata about a test detail for inclusion in comparison results.
    /// </summary>
    public class TestDetailMetadata
    {
        /// <summary>
        /// The unique identifier for the test detail.
        /// </summary>
        public int DetailID { get; set; }

        /// <summary>
        /// The natural language question being tested.
        /// </summary>
        public string Question { get; set; }

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
        /// Indicates whether the test was successful.
        /// </summary>
        public bool Success { get; set; }

        // Add this property to the DatasetComparisonResult class in DynamicDashboardCommon/Models/TestAutomation/DatasetComparisonResult.cs

        /// <summary>
        /// Metadata about the test detail this comparison relates to
        /// </summary>
        public TestDetailMetadata TestDetail { get; set; }
    }
}