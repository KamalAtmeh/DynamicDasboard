using System;

namespace DynamicDashboardCommon.Models
{
    /// <summary>
    /// Represents a database entity with connection details and type information.
    /// </summary>
    public class Database
    {
        public int DatabaseID { get; set; }
        public string Name { get; set; }
        public int TypeID { get; set; } // 1 = SQL Server, 2 = MySQL, 3 = Oracle
        public string ConnectionString { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Description { get; set; }
        public int CreatedBy { get; set; } // User who created the database connection
        public string DBCreationScript { get; set; } // Optional: Script to create the database
    }
}