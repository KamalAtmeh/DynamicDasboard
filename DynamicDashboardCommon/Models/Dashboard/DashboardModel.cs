using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DynamicDashboardCommon.Models
{
    /// <summary>
    /// Represents a configured dashboard in the system.
    /// </summary>
    public class DashboardModel
    {
        /// <summary>
        /// Gets or sets the unique identifier for the dashboard.
        /// </summary>
        public int DashboardID { get; set; }

        /// <summary>
        /// Gets or sets the title of the dashboard.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the description of the dashboard.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the dashboard's layout configuration.
        /// </summary>
        public string LayoutConfig { get; set; }

        /// <summary>
        /// Gets or sets the components included in this dashboard.
        /// </summary>
        public List<DashboardComponent> Components { get; set; } = new List<DashboardComponent>();

        /// <summary>
        /// Gets or sets the database ID this dashboard is associated with.
        /// </summary>
        public int DatabaseID { get; set; }

        /// <summary>
        /// Gets or sets the user ID of the creator.
        /// </summary>
        public int CreatedBy { get; set; }

        /// <summary>
        /// Gets or sets the creation timestamp.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the last update timestamp.
        /// </summary>
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets whether the dashboard is featured in the marketplace.
        /// </summary>
        public bool IsFeatured { get; set; }

        /// <summary>
        /// Gets or sets the dashboard's category ID.
        /// </summary>
        public int CategoryID { get; set; }

        /// <summary>
        /// Gets or sets the category name (for display purposes).
        /// Not stored in database.
        /// </summary>
        
        public string CategoryName { get; set; }

        /// <summary>
        /// Gets or sets the dashboard's sharing status.
        /// </summary>
        public DashboardSharingStatus SharingStatus { get; set; } = DashboardSharingStatus.Private;

        /// <summary>
        /// Gets or sets the refresh interval in seconds.
        /// </summary>
        public int RefreshInterval { get; set; } = 0;

        /// <summary>
        /// Gets or sets the serialized filters configuration.
        /// </summary>
        public string FiltersConfig { get; set; }

        /// <summary>
        /// Gets or sets the tags for this dashboard.
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets whether this dashboard was AI-generated.
        /// </summary>
        public bool IsAIGenerated { get; set; }

        /// <summary>
        /// Gets or sets the validation status for AI-generated dashboards.
        /// </summary>
        public DashboardValidationStatus ValidationStatus { get; set; } = DashboardValidationStatus.Draft;
    }

    /// <summary>
    /// Enum representing dashboard sharing status.
    /// </summary>
    public enum DashboardSharingStatus
    {
        Private = 1,
        Shared = 2,
        Public = 3
    }

    /// <summary>
    /// Enum representing dashboard validation status.
    /// </summary>
    public enum DashboardValidationStatus
    {
        Draft = 1,
        PendingValidation = 2,
        Validated = 3,
        Rejected = 4
    }

    public class CustomChartRequest
    {
        public int DatabaseId { get; set; }
        public string Question { get; set; }
    }


}