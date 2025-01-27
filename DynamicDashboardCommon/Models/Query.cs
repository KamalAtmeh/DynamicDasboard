using System;

namespace DynamicDashboardCommon.Models
{
    /// <summary>
    /// Represents a SQL query and its associated metadata.
    /// </summary>
    public class Query
    {
        /// <summary>
        /// Gets or sets the unique identifier for the query.
        /// </summary>
        public int QueryID { get; set; } // Primary key (if storing in DB)

        /// <summary>
        /// Gets or sets the SQL query string.
        /// </summary>
        public string QueryText { get; set; } // The SQL query string

        /// <summary>
        /// Gets or sets the timestamp when the query was executed.
        /// </summary>
        public DateTime ExecutedAt { get; set; } // Timestamp of execution

        /// <summary>
        /// Gets or sets the user ID of the person who executed the query. Nullable.
        /// </summary>
        public int? ExecutedBy { get; set; } // UserID of the executor (nullable)

        /// <summary>
        /// Gets or sets the type of database where the query was executed (e.g., SQLServer, MySQL, Oracle).
        /// </summary>
        public string DatabaseType { get; set; } // Type of database (e.g., SQLServer, MySQL, Oracle)

        /// <summary>
        /// Gets or sets the serialized result of the query execution. Optional, for logging purposes.
        /// </summary>
        public string Result { get; set; } // Serialized result (optional, for logging purposes)
    }
}
