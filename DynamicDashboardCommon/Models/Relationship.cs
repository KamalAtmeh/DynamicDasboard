using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicDashboardCommon.Models
{
    public class Relationship
    {
        public int RelationshipID { get; set; } // Primary key

        // Foreign key to the source table
        public int TableID { get; set; }

        // Foreign key to the column in the source table
        public int ColumnID { get; set; }

        // Foreign key to the related table
        public int RelatedTableID { get; set; }

        // Foreign key to the column in the related table
        public int RelatedColumnID { get; set; }

        // Type of relationship (e.g., "One-to-Many", "Many-to-Many", "One-to-One")
        public string RelationshipType { get; set; }

        // Optional: Description of the relationship (for admin use)
        public string Description { get; set; }

        // Optional: Indicates if the relationship is enforced by a database constraint
        public bool IsEnforced { get; set; }

        // Optional: Timestamp for when the relationship was created
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Optional: User who created the relationship
        public int CreatedBy { get; set; }
    }
}
