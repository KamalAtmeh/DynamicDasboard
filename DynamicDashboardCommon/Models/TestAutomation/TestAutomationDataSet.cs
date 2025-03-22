namespace DynamicDashboardCommon.Models
{
    /// <summary>
    /// Represents a dataset (either expected or actual) for a test case.
    /// </summary>
    public class TestAutomationDataset
    {
        /// <summary>
        /// The unique identifier for the dataset.
        /// </summary>
        public int DatasetID { get; set; }

        /// <summary>
        /// The ID of the test detail this dataset belongs to.
        /// </summary>
        public int DetailID { get; set; }

        /// <summary>
        /// Indicates whether this is the expected dataset (true) or the actual generated dataset (false).
        /// </summary>
        public bool IsExpected { get; set; }

        /// <summary>
        /// The JSON representation of the dataset.
        /// </summary>
        public string DatasetJSON { get; set; }

        /// <summary>
        /// The number of rows in the dataset.
        /// </summary>
        public int? RowCount { get; set; }

        /// <summary>
        /// The number of columns in the dataset.
        /// </summary>
        public int? ColumnCount { get; set; }

        /// <summary>
        /// JSON array of column names.
        /// </summary>
        public string ColumnNames { get; set; }

        /// <summary>
        /// Hash value for quick dataset comparison.
        /// </summary>
        public string DataHash { get; set; }

        /// <summary>
        /// Navigation property for the parent test detail.
        /// </summary>
        public virtual TestAutomationDetail Detail { get; set; }
    }
}