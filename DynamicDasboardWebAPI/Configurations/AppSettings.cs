namespace DynamicDasboardWebAPI.Configurations
{
    /// <summary>
    /// Configuration settings for different database connections.
    /// </summary>
    public class AppSettings
    {
        /// <summary>
        /// Gets or sets the connection string for SQL Server using Windows Authentication.
        /// </summary>
        public string SQLServerWindowsAuth { get; set; }

        /// <summary>
        /// Gets or sets the connection string for MySQL database.
        /// </summary>
        public string MySQL { get; set; }

        /// <summary>
        /// Gets or sets the connection string for Oracle database.
        /// </summary>
        public string Oracle { get; set; }
    }
}
