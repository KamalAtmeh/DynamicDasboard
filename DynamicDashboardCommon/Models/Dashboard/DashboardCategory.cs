using System;

namespace DynamicDashboardCommon.Models
{
    /// <summary>
    /// Represents a business category for dashboards.
    /// </summary>
    public class DashboardCategory
    {
        /// <summary>
        /// Gets or sets the unique identifier for the category.
        /// </summary>
        public int CategoryID { get; set; }

        /// <summary>
        /// Gets or sets the name of the category.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the description of the category.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the display order for UI presentation.
        /// </summary>
        public int DisplayOrder { get; set; }

        /// <summary>
        /// Gets or sets the icon CSS class for the category.
        /// </summary>
        public string IconClass { get; set; }

        /// <summary>
        /// Gets or sets when this category was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets whether this category is active.
        /// </summary>
        public bool IsActive { get; set; } = true;
    }
}