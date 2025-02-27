using System;

namespace DynamicDashboardCommon.Models
{
    public class NlQueryRequest
    {
        public int DatabaseId { get; set; }
        public string Question { get; set; }
        public string ConnectionString { get; set; } // Added this to fix CS0177

        public string DatabaseType { get; set; } // Changed from DatabaseType to DbType
    }
}