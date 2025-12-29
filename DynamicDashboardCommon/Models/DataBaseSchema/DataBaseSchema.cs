using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DynamicDashboardCommon.Models
{
    // The top-level JSON object stored in DatabaseSchemas.SchemaData
 public class DatabaseSchema
    {
        // ============================================
        // DATABASE COLUMNS
        // ============================================
        public int ID { get; set; }
        public int DataBaseID { get; set; }
        public string Name { get; set; }
        public int Status { get; set; }
        
        /// <summary>
        /// Raw JSON string - MUST be ignored to prevent recursive serialization
        /// </summary>
        [JsonIgnore]
        public string SchemaData { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public DateTime ModifiedAt { get; set; }

        // ============================================
        // JSON CONTENT (stored inside SchemaData)
        // ============================================
        public VersionInfo Version { get; set; }
        public Config Config { get; set; }
        public List<TableSchema> Tables { get; set; }
        public List<RelationshipSchema> Relationships { get; set; }
        public AnalysisResults AnalysisResults { get; set; }
        public List<VersionHistory> VersionHistory { get; set; }
        public List<TermMapping> TermMappings { get; set; } = new List<TermMapping>();
    }

    public class VersionInfo
    {
        public string Number { get; set; }
        public string Description { get; set; }
        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }
    }

    public class Config
    {
        public bool CaseSensitive { get; set; }
        public string Collation { get; set; }
        public SchemaAnalysisSettings SchemaAnalysisSettings { get; set; }
    }

    public class SchemaAnalysisSettings
    {
        public bool AutoDetectRelationships { get; set; }
        public double ConfidenceThreshold { get; set; }
    }

    public class TableSchema
    {
        public string ID { get; set; }
        public string Status { get; set; } = string.Empty;
        public string DBName { get; set; } = string.Empty;
        public string FriendlyName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<ColumnSchema> Columns { get; set; } = new List<ColumnSchema>();
        public List<IndexSchema> Indexes { get; set; } = new List<IndexSchema>();

        public List<string> Synonyms { get; set; } = new List<string>();

        public bool IsActive { get; set; } = true;

        public int TotalColumns { get; set; } = 0;
    }

    public class ColumnSchema
    {
        public string ID { get; set; }
        public string DBName { get; set; } = string.Empty;
        public string FriendlyName { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public bool IsNullable { get; set; } 
        public bool IsPrimaryKey { get; set; }
        public bool IsLookup { get; set; }
        public string Description { get; set; } = string.Empty;
        public UiConfig UIConfig { get; set; } = new UiConfig();
        public List<ConstraintSchema> Constraints { get; set; } = new List<ConstraintSchema>();
        public List<string> Synonyms { get; set; } = new List<string>();

        public bool IsActive { get; set; } = true;
    }

    public class UiConfig
    {
        public bool Visible { get; set; } = true;
        public int Order { get; set; } = 0;
        public string DefaultSort { get; set; } = string.Empty;
    }

    public class ConstraintSchema
    {
        public string Type { get; set; }
        public string Expression { get; set; }
    }

    public class IndexSchema
    {
        public string Name { get; set; }
        public List<string> Columns { get; set; }
        public bool IsUnique { get; set; }
    }

    public class RelationshipSchema
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public string Type { get; set; } // e.g. "one-to-many"
        public string Status { get; set; }
        public RelationshipDetails Source { get; set; }
        public RelationshipDetails Target { get; set; }
        public bool Enforced { get; set; }
        public RelationshipMetadata Metadata { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class RelationshipDetails
    {
        public string TableID { get; set; }      // Table ID
        public string TableName { get; set; }  // Table friendly name
        public string ColumnID { get; set; }     // Column ID
        public string ColumnName { get; set; } // Column friendly name
    }

    public class RelationshipMetadata
    {
        public double Confidence { get; set; }
        public DateTime DiscoveredAt { get; set; }
        public DateTime LastValidated { get; set; }
    }

    public class AnalysisResults
    {
        public DateTime LastAnalyzed { get; set; }
        public List<PotentialConflict> PotentialConflicts { get; set; }
        public List<UnclearElement> UnclearElements { get; set; }
        public List<SuggestedRelationship> SuggestedRelationships { get; set; }
        public List<TableDescription> TableDescriptions { get; set; }
        public List<ColumnDescription> ColumnDescriptions { get; set; }
    }

    public class PotentialConflict
    {
        public string Type { get; set; }
        public string ConflictDescription { get; set; }
        public List<ConflictItem> Items { get; set; }
    }

    public class ConflictItem
    {
        public string Name { get; set; }
        public string TableName { get; set; }
        public string SuggestedResolution { get; set; }
    }

    public class UnclearElement
    {
        public string Type { get; set; }
        public string Name { get; set; }
        public string TableName { get; set; }
        public string Issue { get; set; }
        public string Suggestion { get; set; }
    }

    public class SuggestedRelationship
    {
        public string RelationshipType { get; set; }
        public double Confidence { get; set; }
        public RelationshipDetails SourceTable { get; set; }
        public RelationshipDetails TargetTable { get; set; }
        public string Reasoning { get; set; }
    }

    public class TableColumnRef
    {
        public string Table { get; set; }
        public string Column { get; set; }
    }

    public class TableDescription
    {
        public string TableName { get; set; }
        public string SuggestedName { get; set; }
        public string SuggestedDescription { get; set; }
    }

    public class ColumnDescription
    {
        public string TableName { get; set; }
        public string ColumnName { get; set; }
        public string SuggestedName { get; set; }
        public string SuggestedDescription { get; set; }
        public bool IsLookupColumn { get; set; }
    }

    public class VersionHistory
    {
        public string Version { get; set; }
        public DateTime Date { get; set; }
        public string Changes { get; set; }
    }

    public class DatabaseMetadataDto
    {
        public int DatabaseID { get; set; }
        public List<TableMetadataDto> Tables { get; set; }
    }

    public class TableMetadataDto
    {
        public Table Table { get; set; }
        public IEnumerable<Column> Columns { get; set; }
        public IEnumerable<Relationship> Relationships { get; set; }
    }

    /// <summary>
    /// Model for schema analysis requests
    /// </summary>
    public class SchemaAnalysisRequest
    {
        public int DatabaseId { get; set; }
        public string SchemaString { get; set; }
        public string AnalysisMode { get; set; }
    }
}
