using System.Collections.Generic;

namespace DynamicDashboardCommon.Models
{
    /// <summary>
    /// Request model for AI assistant chat
    /// </summary>
    public class AssistantChatRequest
    {
        public int DashboardId { get; set; }
        public int DatabaseId { get; set; }
        public List<DashboardComponent> CurrentComponents { get; set; }
    }

    /// <summary>
    /// Response model for AI assistant suggestions
    /// </summary>
    public class AssistantSuggestionResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public List<ComponentSuggestion> Suggestions { get; set; }
    }

    /// <summary>
    /// Individual component suggestion
    /// </summary>
    public class ComponentSuggestion
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Icon { get; set; }
        public int DataViewingTypeID { get; set; }
        public string ChartType { get; set; }
        public string SqlTemplate { get; set; }
        public int GridWidth { get; set; }
        public int GridHeight { get; set; }
    }
}
