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
        public string DataBaseViewingName { get; set; } = string.Empty;

        /// <summary>
        /// Type ID of the database (Foreign key to DatabaseTypes)
        /// </summary>
        public int TypeID { get; set; }

        /// <summary>
        /// Server address for the database connection
        /// </summary>
        public string ServerAddress { get; set; } = string.Empty;

        /// <summary>
        /// Actual name of the database
        /// </summary>
        public string DatabaseName { get; set; } = string.Empty;

        /// <summary>
        /// Port number for the database connection
        /// </summary>
        public int Port { get; set; } = 0;

        /// <summary>
        /// Username for database authentication
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Encrypted credentials for secure storage
        /// </summary>
        public string EncryptedCredentials { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp of database connection creation
        /// </summary>
        public DateTime? CreatedAt { get; set; }

        /// <summary>
        /// User ID of the creator
        /// </summary>
        public int CreatedBy { get; set; } = 0;

        /// <summary>
        /// Description of the database connection
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Indicates if the database connection is active
        /// </summary>
        public bool IsActive { get; set; } = true;

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
        public string DBCreationScript { get; set; } = string.Empty;

        /// <summary>
        /// Connection string for the database
        /// </summary>
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// Type name of the database (not a database column, but useful for display)
        /// </summary>
        public string DatabaseTypeName { get; set; } = string.Empty;
    }
}