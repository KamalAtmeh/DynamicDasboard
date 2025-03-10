using System;

namespace DynamicDashboardCommon.Models
{
    // This entity represents the database schema saved as a JSON document.
    // It maps to the DatabaseSchemas table.
    public class DatabaseJsonSchema
    {
        public int Id { get; set; }                   // Primary key (Identity)
        public string Name { get; set; }              // Database name
        public int Status { get; set; }               // Status (e.g., active=1, inactive=0, draft=2)
        public string SchemaData { get; set; }        // JSON data (must be valid JSON)
        public DateTime CreatedAt { get; set; }       // Creation timestamp
        public DateTime ModifiedAt { get; set; }      // Last modified timestamp
    }
}
