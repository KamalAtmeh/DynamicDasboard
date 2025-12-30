using System.Collections.Generic;

namespace DynamicDashboardCommon.Models
{
    /// <summary>
    /// Represents a static dashboard template configuration
    /// </summary>
    public class DashboardTemplate
    {
        /// <summary>
        /// Unique identifier for the template
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Display name of the template
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Description of what the template provides
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Category of the template (Executive, Operations, Finance, etc.)
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// URL to the template thumbnail image
        /// </summary>
        public string ThumbnailUrl { get; set; }

        /// <summary>
        /// Total height of the canvas in grid rows
        /// </summary>
        public int TotalHeight { get; set; }

        /// <summary>
        /// List of component slots in this template
        /// </summary>
        public List<TemplateComponentSlot> Components { get; set; } = new List<TemplateComponentSlot>();
    }

    /// <summary>
    /// Represents a component slot within a template
    /// </summary>
    public class TemplateComponentSlot
    {
        /// <summary>
        /// Slot number for ordering
        /// </summary>
        public int Slot { get; set; }

        /// <summary>
        /// Type of component (kpi, chart, table, card, label)
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Chart type if this is a chart component (line, bar, pie, area, etc.)
        /// </summary>
        public string ChartType { get; set; }

        /// <summary>
        /// Default title for this component
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Default description for this component
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// X position in the grid (0-11 for 12-column grid)
        /// </summary>
        public int GridX { get; set; }

        /// <summary>
        /// Y position in the grid (row number)
        /// </summary>
        public int GridY { get; set; }

        /// <summary>
        /// Width in grid columns (1-12)
        /// </summary>
        public int GridWidth { get; set; }

        /// <summary>
        /// Height in grid rows
        /// </summary>
        public int GridHeight { get; set; }

        /// <summary>
        /// Natural language description of what this component should show
        /// </summary>
        public string QueryIntent { get; set; }

        /// <summary>
        /// Suggested aggregation function for this component (COUNT, SUM, AVG, PERCENTAGE)
        /// </summary>
        public string SuggestedAggregation { get; set; }
    }

    /// <summary>
    /// Container for all dashboard templates
    /// </summary>
    public class DashboardTemplateCollection
    {
        /// <summary>
        /// List of available templates
        /// </summary>
        public List<DashboardTemplate> Templates { get; set; } = new List<DashboardTemplate>();
    }
}
