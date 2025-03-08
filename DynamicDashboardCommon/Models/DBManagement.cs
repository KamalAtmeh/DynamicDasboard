using System;
using System.Collections.Generic;

namespace DynamicDashboardCommon.Models
{
    /// <summary>
    /// Represents the result of a schema analysis operation.
    /// </summary>
    public class SchemaAnalysisResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether the analysis was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the error message if the analysis failed.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the raw LLM response for debugging.
        /// </summary>
        public string RawLLMResponse { get; set; }

        /// <summary>
        /// Gets or sets the analysis data.
        /// </summary>
        public SchemaAnalysisData AnalysisData { get; set; }
    }

    /// <summary>
    /// Contains the data from a schema analysis operation.
    /// </summary>
    public class SchemaAnalysisData
    {
        /// <summary>
        /// Gets or sets the suggested descriptions for tables.
        /// </summary>
        public List<TableDescription> TableDescriptions { get; set; } = new List<TableDescription>();

        /// <summary>
        /// Gets or sets the suggested descriptions for columns.
        /// </summary>
        public List<ColumnDescription> ColumnDescriptions { get; set; } = new List<ColumnDescription>();

        /// <summary>
        /// Gets or sets the potential conflicts detected in the schema.
        /// </summary>
        public List<PotentialConflict> PotentialConflicts { get; set; } = new List<PotentialConflict>();

        /// <summary>
        /// Gets or sets the suggested relationships between tables.
        /// </summary>
        public List<SuggestedRelationship> SuggestedRelationships { get; set; } = new List<SuggestedRelationship>();

        /// <summary>
        /// Gets or sets the unclear elements detected in the schema.
        /// </summary>
        public List<UnclearElement> UnclearElements { get; set; } = new List<UnclearElement>();
    }

    /// <summary>
    /// Represents a suggested description for a table.
    /// </summary>
    public class TableDescription
    {
        /// <summary>
        /// Gets or sets the name of the table.
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// Gets or sets the suggested user-friendly name for the table.
        /// </summary>
        public string SuggestedName { get; set; }

        /// <summary>
        /// Gets or sets the suggested description for the table.
        /// </summary>
        public string SuggestedDescription { get; set; }
    }

    /// <summary>
    /// Represents a suggested description for a column.
    /// </summary>
    public class ColumnDescription
    {
        /// <summary>
        /// Gets or sets the name of the table containing the column.
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// Gets or sets the name of the column.
        /// </summary>
        public string ColumnName { get; set; }

        /// <summary>
        /// Gets or sets the suggested user-friendly name for the column.
        /// </summary>
        public string SuggestedName { get; set; }

        /// <summary>
        /// Gets or sets the suggested description for the column.
        /// </summary>
        public string SuggestedDescription { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the column is a lookup column.
        /// </summary>
        public bool IsLookupColumn { get; set; }
    }

    /// <summary>
    /// Represents a potential conflict in the schema.
    /// </summary>
    public class PotentialConflict
    {
        /// <summary>
        /// Gets or sets the type of the conflict ("Table" or "Column").
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the items involved in the conflict.
        /// </summary>
        public List<ConflictItem> Items { get; set; } = new List<ConflictItem>();

        /// <summary>
        /// Gets or sets the description of the conflict.
        /// </summary>
        public string ConflictDescription { get; set; }
    }

    /// <summary>
    /// Represents an item involved in a conflict.
    /// </summary>
    public class ConflictItem
    {
        /// <summary>
        /// Gets or sets the name of the item.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the name of the table (for columns).
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// Gets or sets the suggested resolution for the conflict.
        /// </summary>
        public string SuggestedResolution { get; set; }
    }

    /// <summary>
    /// Represents a suggested relationship between tables.
    /// </summary>
    public class SuggestedRelationship
    {
        /// <summary>
        /// Gets or sets the name of the source table.
        /// </summary>
        public string SourceTable { get; set; }

        /// <summary>
        /// Gets or sets the name of the source column.
        /// </summary>
        public string SourceColumn { get; set; }

        /// <summary>
        /// Gets or sets the name of the target table.
        /// </summary>
        public string TargetTable { get; set; }

        /// <summary>
        /// Gets or sets the name of the target column.
        /// </summary>
        public string TargetColumn { get; set; }

        /// <summary>
        /// Gets or sets the type of the relationship.
        /// </summary>
        public string RelationshipType { get; set; }

        /// <summary>
        /// Gets or sets the confidence score of the suggested relationship.
        /// </summary>
        public double Confidence { get; set; }

        /// <summary>
        /// Gets or sets the reasoning behind the suggested relationship.
        /// </summary>
        public string Reasoning { get; set; }
    }

    /// <summary>
    /// Represents an unclear element in the schema.
    /// </summary>
    public class UnclearElement
    {
        /// <summary>
        /// Gets or sets the type of the element ("Table" or "Column").
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the name of the element.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the name of the table (for columns).
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// Gets or sets the issue with the element.
        /// </summary>
        public string Issue { get; set; }

        /// <summary>
        /// Gets or sets the suggested improvement for the element.
        /// </summary>
        public string Suggestion { get; set; }
    }
}