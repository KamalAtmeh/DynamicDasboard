using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;



namespace DynamicDashboardCommon.Models.TestAutomation
{

    /// <summary>
    /// Represents the result of comparing two datasets.
    /// Provides detailed information about the comparison including functional equivalence
    /// and structural differences.
    /// </summary>
    public class DatasetComparisonResult
    {
        /// <summary>
        /// The expected dataset for comparison
        /// </summary>
        public List<Dictionary<string, object>> Expected { get; set; }

        /// <summary>
        /// The actual dataset for comparison
        /// </summary>
        public List<Dictionary<string, object>> Actual { get; set; }

        /// <summary>
        /// Whether the datasets are functionally equivalent (common columns have matching values)
        /// </summary>
        public bool IsEquivalent { get; set; }

        /// <summary>
        /// Total number of rows compared
        /// </summary>
        public int TotalRowsCompared { get; set; }

        /// <summary>
        /// Number of rows with matching values for common columns
        /// </summary>
        public int MatchingRows { get; set; }

        /// <summary>
        /// Number of rows with at least one difference in common columns
        /// </summary>
        public int DifferentRows { get; set; }

        /// <summary>
        /// Whether the datasets have different column structures
        /// </summary>
        public bool HasStructuralDifferences { get; set; }

        /// <summary>
        /// Columns that exist in both datasets (case-insensitive matching)
        /// </summary>
        public List<string> CommonColumns { get; set; }

        /// <summary>
        /// Columns that exist only in the expected dataset
        /// </summary>
        public List<string> UniqueExpectedColumns { get; set; }

        /// <summary>
        /// Columns that exist only in the actual dataset
        /// </summary>
        public List<string> UniqueActualColumns { get; set; }

        /// <summary>
        /// Detailed information about differences between datasets
        /// </summary>
        public List<DatasetDifference> Differences { get; set; }

        /// <summary>
        /// Human-readable explanation of the comparison result
        /// </summary>
        public string ComparisonSummary { get; set; }

        /// <summary>
        /// Constructor with default values
        /// </summary>
        public DatasetComparisonResult()
        {
            Expected = new List<Dictionary<string, object>>();
            Actual = new List<Dictionary<string, object>>();
            CommonColumns = new List<string>();
            UniqueExpectedColumns = new List<string>();
            UniqueActualColumns = new List<string>();
            Differences = new List<DatasetDifference>();
            ComparisonSummary = "No comparison performed";
        }

       

        /// <summary>
        /// Metadata about the test detail this comparison relates to
        /// </summary>
        public TestDetailMetadata TestDetail { get; set; }
    }

    /// <summary>
    /// Represents a specific difference between datasets
    /// </summary>
    public class DatasetDifference
    {
        /// <summary>
        /// Zero-based index of the row with the difference
        /// </summary>
        public int RowIndex { get; set; }

        /// <summary>
        /// Name of the column with the difference
        /// </summary>
        public string ColumnName { get; set; }

        /// <summary>
        /// The value in the expected dataset
        /// </summary>
        public object ExpectedValue { get; set; }

        /// <summary>
        /// The value in the actual dataset
        /// </summary>
        public object ActualValue { get; set; }

        /// <summary>
        /// Type of difference (structural, value, type, etc.)
        /// </summary>
        public string DifferenceType { get; set; }

        /// <summary>
        /// Human-readable description of the difference
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Description { get; set; }
    }
}
