// Add to DynamicDashboardCommon/Models/DataBaseSchema/TermMapping.cs
namespace DynamicDashboardCommon.Models
{
    public class TermMapping
    {
        public string ID { get; set; } = Guid.NewGuid().ToString();
        public string BusinessTerm { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public TermMappingType Type { get; set; } = TermMappingType.DirectColumn;

        // For direct column mappings
        public string TableId { get; set; } = string.Empty;
        public string ColumnId { get; set; } = string.Empty;

        // For calculated fields
        public string Formula { get; set; } = string.Empty;
        public List<TermMappingDependency> Dependencies { get; set; } = new List<TermMappingDependency>();

        public bool IsConfirmed { get; set; } = false;

        // For filter conditions
        public string FilterCondition { get; set; } = string.Empty;

        public List<string> Synonyms { get; set; } = new List<string>();
        public bool IsActive { get; set; } = true;
        public bool IsLLMSuggested { get; set; } = false; // Indicates if this was suggested by AI
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ModifiedAt { get; set; }
    }

    public class TermMappingDependency
    {
        public string TableId { get; set; }
        public string ColumnId { get; set; }
        public string TableName { get; set; }
        public string ColumnName { get; set; }
    }

    public enum TermMappingType
    {
        DirectColumn,      // Maps directly to a single column
        CalculatedField,   // Maps to a calculated field
        Aggregate,         // Represents an aggregation (COUNT, SUM, etc.)
        FilterCondition    // Represents a filtering condition
    }

    public class TermSuggestion
    {
        public string BusinessTerm { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        public List<DependencySuggestion> Dependencies { get; set; }
        public string Formula { get; set; }
        public string FilterCondition { get; set; }
        public List<string> Synonyms { get; set; }
    }

    public class DependencySuggestion
    {
        public string TableName { get; set; }
        public string ColumnName { get; set; }
    }

    public class TermMappingResponse
    {
        public Dictionary<string, string> TermMappings { get; set; }
    }
    public class FormulaValidationRequest
    {
        public string Formula { get; set; }
    }
}