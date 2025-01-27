using System;

namespace DynamicDashboardCommon.Models
{
    /// <summary>
    /// Represents a table in the dynamic dashboard system.
    /// This class holds metadata about a database table, including its ID, 
    /// the database it belongs to, its actual name in the database, 
    /// a user-friendly name, and an admin-provided description.
    /// </summary>
    public class Table
    {
        /// <summary>
        /// Gets or sets the unique identifier for the table.
        /// </summary>
        public int TableID { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the database this table belongs to.
        /// This is a foreign key to the Database table.
        /// </summary>
        public int DatabaseID { get; set; } // Foreign key to the Database table

        /// <summary>
        /// Gets or sets the actual name of the table in the database.
        /// </summary>
        public string DBTableName { get; set; } // Actual table name in the database

        /// <summary>
        /// Gets or sets the user-friendly name for the table.
        /// </summary>
        public string AdminTableName { get; set; } // User-friendly name for the table

        /// <summary>
        /// Gets or sets the description provided by the admin for the table.
        /// </summary>
        public string AdminDescription { get; set; } // Description provided by the admin
    }


}
