using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicDashboardCommon.Models
{
    /// <summary>
    /// Represents a relationship between tables in a dynamic dashboard system.
    /// </summary>
    public class Relationship
    {
        /// <summary>
        /// Gets or sets the primary key of the relationship.
        /// </summary>
        public int RelationshipID { get; set; } // Primary key

        /// <summary>
        /// Gets or sets the foreign key to the source table.
        /// </summary>
        public int TableID { get; set; }

        /// <summary>
        /// Gets or sets the foreign key to the column in the source table.
        /// </summary>
        public int ColumnID { get; set; }

        /// <summary>
        /// Gets or sets the foreign key to the related table.
        /// </summary>
        public int RelatedTableID { get; set; }

        /// <summary>
        /// Gets or sets the foreign key to the column in the related table.
        /// </summary>
        public int RelatedColumnID { get; set; }

        /// <summary>
        /// Gets or sets the type of relationship (e.g., "One-to-Many", "Many-to-Many", "One-to-One").
        /// </summary>
        public string RelationshipType { get; set; }

        /// <summary>
        /// Gets or sets the description of the relationship (for admin use).
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the relationship is enforced by a database constraint.
        /// </summary>
        public bool IsEnforced { get; set; }

        /// <summary>
        /// Gets or sets the timestamp for when the relationship was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the user who created the relationship.
        /// </summary>
        public int CreatedBy { get; set; }
    }
}
