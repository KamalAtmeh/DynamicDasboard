using System;

namespace DynamicDashboardCommon.Models
{
    public class Table
    {
        public int TableID { get; set; }
        public int DatabaseID { get; set; } // Foreign key to the Database table
        public string DBTableName { get; set; } // Actual table name in the database
        public string AdminTableName { get; set; } // User-friendly name for the table
        public string AdminDescription { get; set; } // Description provided by the admin
    }


}
