// ============================================================================
// DashboardBuilderModels.cs
// Shared models for Dashboard Builder and AI Component Assistant
// Location: DynamicDashboardFE/Models/DashboardBuilderModels.cs
// ============================================================================

namespace DynamicDashboardCommon.Models
{
    /// <summary>
    /// Represents a component template available in the component library
    /// </summary>
    public class BuilderComponentTemplate
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = "fa-chart-bar";
        public string Category { get; set; } = string.Empty;
        public int DataViewingTypeID { get; set; }
    }

    /// <summary>
    /// Represents a pre-built dashboard layout template
    /// </summary>
    public class BuilderDashboardTemplate
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = "fas fa-th-large";
        public string LayoutClass { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<BuilderComponentSlot> ComponentSlots { get; set; } = new();
    }

    /// <summary>
    /// Represents a slot within a dashboard template where a component can be placed
    /// </summary>
    public class BuilderComponentSlot
    {
        public string Id { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int GridX { get; set; }
        public int GridY { get; set; }
        public int GridWidth { get; set; } = 4;
        public int GridHeight { get; set; } = 3;
    }

    /// <summary>
    /// Represents the result of AI component generation
    /// </summary>
    public class AIGeneratedComponent
    {
        public string SlotId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ComponentType { get; set; } = string.Empty;
        public string QueryText { get; set; } = string.Empty;
        public string QueryIntent { get; set; } = string.Empty;
        public string VisualizationConfig { get; set; } = "{}";
        public bool IsAccepted { get; set; }
        public int GridX { get; set; }
        public int GridY { get; set; }
        public int GridWidth { get; set; }
        public int GridHeight { get; set; }
    }
}
