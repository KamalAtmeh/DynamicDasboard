using System;
using System.Collections.Generic;

namespace DynamicDashboardCommon.Models
{
    // The top-level JSON object stored in DatabaseSchemas.SchemaData
    public class DatabaseSchema
    {
        public int ID { get; set; }
        public int DataBaseID { get; set; }
        public string Name { get; set; }
        public int Status { get; set; }
        public VersionInfo Version { get; set; }
        public Config Config { get; set; }
        public List<TableSchema> Tables { get; set; }
        public List<RelationshipSchema> Relationships { get; set; }
        public AnalysisResults AnalysisResults { get; set; }
        public List<VersionHistory> VersionHistory { get; set; }
        public string SchemaData { get; set; }        // JSON data (must be valid JSON)
        public DateTime CreatedAt { get; set; }       // Creation timestamp
        public DateTime ModifiedAt { get; set; }      // Last modified timestamp
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
        public string Status { get; set; }
        public string DBName { get; set; }
        public string FriendlyName { get; set; }
        public string Description { get; set; }
        public List<ColumnSchema> Columns { get; set; }
        public List<IndexSchema> Indexes { get; set; }

        public List<string> Synonyms { get; set; } = new List<string>();
    }

    public class ColumnSchema
    {
        public string ID { get; set; }
        public string DBName { get; set; }
        public string FriendlyName { get; set; }
        public string DataType { get; set; }
        public bool IsNullable { get; set; }
        public bool IsPrimaryKey { get; set; }
        public bool IsLookup { get; set; }
        public string Description { get; set; }
        public UiConfig UIConfig { get; set; }
        public List<ConstraintSchema> Constraints { get; set; }
        public List<string> Synonyms { get; set; } = new List<string>();
    }

    public class UiConfig
    {
        public bool Visible { get; set; }
        public int Order { get; set; }
        public string DefaultSort { get; set; }
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
}
