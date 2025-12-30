using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DynamicDashboardCommon.Models
{
    /// <summary>
    /// Represents a visualization component within a dashboard.
    /// </summary>
    public class DashboardComponent
    {
        /// <summary>
        /// Gets or sets the unique identifier for the component.
        /// </summary>
        public int ComponentID { get; set; }

        /// <summary>
        /// Gets or sets the dashboard ID this component belongs to.
        /// </summary>
        public int DashboardID { get; set; }

        /// <summary>
        /// Gets or sets the title of the component.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the description of the component.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the type of the component for visualization.
        /// </summary>
        public int DataViewingTypeID { get; set; }

        /// <summary>
        /// Gets or sets the component type name (for display purposes).
        /// Not stored in database.
        /// </summary>
        
        public string DataViewingTypeName { get; set; }

        /// <summary>
        /// Gets or sets the type of chart visualization.
        /// Only applicable when DataViewingTypeID is Chart.
        /// </summary>
        public string ChartType { get; set; }

        /// <summary>
        /// Gets or sets the component's position in the layout grid (X coordinate).
        /// </summary>
        public int GridX { get; set; }

        /// <summary>
        /// Gets or sets the component's position in the layout grid (Y coordinate).
        /// </summary>
        public int GridY { get; set; }

        /// <summary>
        /// Gets or sets the component's width in the layout grid.
        /// </summary>
        public int GridWidth { get; set; } = 4;

        /// <summary>
        /// Gets or sets the component's height in the layout grid.
        /// </summary>
        public int GridHeight { get; set; } = 4;

        /// <summary>
        /// Gets or sets the SQL query used to populate this component.
        /// </summary>
        public string QueryText { get; set; }

        /// <summary>
        /// Gets or sets the natural language description of what the component displays.
        /// </summary>
        public string QueryIntent { get; set; }

        /// <summary>
        /// Gets or sets serialized configuration options for the component visualization.
        /// </summary>
        public string VisualizationConfig { get; set; }

        /// <summary>
        /// Gets or sets the parameters for the component's query.
        /// </summary>
        public List<ComponentParameter> Parameters { get; set; } = new List<ComponentParameter>();

        /// <summary>
        /// Gets or sets whether this component has been validated.
        /// </summary>
        public bool IsValidated { get; set; }

        /// <summary>
        /// Gets or sets who validated this component.
        /// </summary>
        public int? ValidatedBy { get; set; }

        /// <summary>
        /// Gets or sets when this component was validated.
        /// </summary>
        public DateTime? ValidatedAt { get; set; }

        /// <summary>
        /// Gets or sets whether this component is visible in the dashboard.
        /// </summary>
        public bool IsVisible { get; set; } = true;

        /// <summary>
        /// Gets or sets whether this component was AI-generated.
        /// </summary>
        public bool IsAIGenerated { get; set; }

        /// <summary>
        /// Gets or sets the refresh interval override in seconds (0 means use dashboard default).
        /// </summary>
        public int RefreshInterval { get; set; } = 0;

        /// <summary>
        /// Gets or sets filter expressions for the component.
        /// </summary>
        public string FilterExpression { get; set; }

        /// <summary>
        /// Gets or sets the creation date of this component.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets when this component was last updated.
        /// </summary>
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}