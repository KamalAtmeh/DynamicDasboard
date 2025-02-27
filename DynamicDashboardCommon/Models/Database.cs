using System;

namespace DynamicDashboardCommon.Models
{
    /// <summary>
    /// Represents a database connection in the system with comprehensive details
    /// </summary>
    public class Database
    {
        /// <summary>
        /// Unique identifier for the database connection
        /// </summary>
        public int DatabaseID { get; set; }

        /// <summary>
        /// Name of the database connection
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Type ID of the database (Foreign key to DatabaseTypes)
        /// </summary>
        public int TypeID { get; set; }

        /// <summary>
        /// Server address for the database connection
        /// </summary>
        public string ServerAddress { get; set; }

        /// <summary>
        /// Actual name of the database
        /// </summary>
        public string DatabaseName { get; set; }

        /// <summary>
        /// Port number for the database connection
        /// </summary>
        public int? Port { get; set; }

        /// <summary>
        /// Username for database authentication
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Encrypted credentials for secure storage
        /// </summary>
        public string EncryptedCredentials { get; set; }

        /// <summary>
        /// Timestamp of database connection creation
        /// </summary>
        public DateTime? CreatedAt { get; set; }

        /// <summary>
        /// User ID of the creator
        /// </summary>
        public int CreatedBy { get; set; }

        /// <summary>
        /// Description of the database connection
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Indicates if the database connection is active
        /// </summary>
        public bool? IsActive { get; set; }

        /// <summary>
        /// Timestamp of the last transaction
        /// </summary>
        public DateTime? LastTransactionDate { get; set; }

        /// <summary>
        /// Indicates the last connection status
        /// </summary>
        public bool? LastConnectionStatus { get; set; }

        /// <summary>
        /// Database creation script
        /// </summary>
        public string DBCreationScript { get; set; }

        /// <summary>
        /// Connection string for the database
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Type name of the database (not a database column, but useful for display)
        /// </summary>
        public string DatabaseTypeName { get; set; }
    }
}