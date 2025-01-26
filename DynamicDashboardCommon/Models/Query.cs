using System;

namespace DynamicDashboardCommon.Models
{
    /// <summary>
    /// Represents a SQL query.
    /// </summary>
    public class Query
    {
        public int QueryID { get; set; } // Primary key (if storing in DB)
        public string QueryText { get; set; } // The SQL query string
        public DateTime ExecutedAt { get; set; } // Timestamp of execution
        public int? ExecutedBy { get; set; } // UserID of the executor (nullable)
        public string DatabaseType { get; set; } // Type of database (e.g., SQLServer, MySQL, Oracle)
        public string Result { get; set; } // Serialized result (optional, for logging purposes)
    }
}
