using System;

namespace DynamicDashboardCommon.Models
{
    /// <summary>
    /// Represents a database entity with connection details and type information.
    /// This class is used to store and manage information about different types of databases
    /// such as SQL Server, MySQL, and Oracle. It includes properties for the database ID, name,
    /// type, connection string, creation date, description, creator, and an optional creation script.
    /// </summary>
    public class Database
    {
        /// <summary>
        /// Gets or sets the unique identifier for the database.
        /// </summary>
        public int DatabaseID { get; set; }

        /// <summary>
        /// Gets or sets the name of the database.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the type identifier for the database.
        /// 1 = SQL Server, 2 = MySQL, 3 = Oracle.
        /// </summary>
        public int TypeID { get; set; } // 1 = SQL Server, 2 = MySQL, 3 = Oracle

        /// <summary>
        /// Gets or sets the connection string used to connect to the database.
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the database entry was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the description of the database.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the user who created the database connection.
        /// </summary>
        public int CreatedBy { get; set; } // User who created the database connection

        /// <summary>
        /// Gets or sets the optional script to create the database.
        /// </summary>
        public string DBCreationScript { get; set; } // Optional: Script to create the database
    }
}