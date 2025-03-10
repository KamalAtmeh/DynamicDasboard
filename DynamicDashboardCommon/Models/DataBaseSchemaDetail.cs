using System;
using System.Collections.Generic;

namespace DynamicDashboardCommon.Models
{
    // The top-level JSON object stored in DatabaseSchemas.SchemaData
    public class DatabaseSchemaDetail
    {
        public int id { get; set; }
        public string name { get; set; }
        public string status { get; set; }
        public VersionInfo version { get; set; }
        public Config config { get; set; }
        public List<TableSchema> tables { get; set; }
        public List<RelationshipSchema> relationships { get; set; }
        public AnalysisResults analysisResults { get; set; }
        public List<VersionHistory> versionHistory { get; set; }
    }

    public class VersionInfo
    {
        public string number { get; set; }
        public string description { get; set; }
        public DateTime created { get; set; }
        public DateTime modified { get; set; }
    }

    public class Config
    {
        public bool caseSensitive { get; set; }
        public string collation { get; set; }
        public SchemaAnalysisSettings schemaAnalysisSettings { get; set; }
    }

    public class SchemaAnalysisSettings
    {
        public bool autoDetectRelationships { get; set; }
        public double confidenceThreshold { get; set; }
    }

    public class TableSchema
    {
        public string id { get; set; }
        public string status { get; set; }
        public string dbName { get; set; }
        public string FriendlyName { get; set; } // Replaces "adminName"
        public string description { get; set; }
        public List<ColumnSchema> columns { get; set; }
        public List<IndexSchema> indexes { get; set; }
    }

    public class ColumnSchema
    {
        public string id { get; set; }
        public string dbName { get; set; }
        public string FriendlyName { get; set; }
        public string dataType { get; set; }
        public bool isNullable { get; set; }
        public bool isPrimaryKey { get; set; }
        public bool isLookup { get; set; }
        public string description { get; set; }
        public UiConfig uiConfig { get; set; }
        public List<ConstraintSchema> constraints { get; set; }
    }

    public class UiConfig
    {
        public bool visible { get; set; }
        public int order { get; set; }
        public string defaultSort { get; set; }
    }

    public class ConstraintSchema
    {
        public string type { get; set; }
        public string expression { get; set; }
    }

    public class IndexSchema
    {
        public string name { get; set; }
        public List<string> columns { get; set; }
        public bool isUnique { get; set; }
    }

    public class RelationshipSchema
    {
        public string id { get; set; }
        public string name { get; set; }
        public string type { get; set; } // e.g. "one-to-many"
        public string status { get; set; }
        public RelationshipEndpoint source { get; set; }
        public RelationshipEndpoint target { get; set; }
        public bool enforced { get; set; }
        public RelationshipMetadata metadata { get; set; }
    }

    public class RelationshipEndpoint
    {
        public string table { get; set; }
        public string column { get; set; }
    }

    public class RelationshipMetadata
    {
        public double confidence { get; set; }
        public DateTime discoveredAt { get; set; }
        public DateTime lastValidated { get; set; }
    }

    public class AnalysisResults
    {
        public DateTime lastAnalyzed { get; set; }

        // Potential conflicts
        public List<PotentialConflict> PotentialConflicts { get; set; }

        // Unclear elements
        public List<UnclearElement> UnclearElements { get; set; }

        // Suggested relationships
        public List<SuggestedRelationship> SuggestedRelationships { get; set; }

        // Table & Column descriptions
        public List<TableDescription> TableDescriptions { get; set; }
        public List<ColumnDescription> ColumnDescriptions { get; set; }
    }

    public class PotentialConflict
    {
        public string Type { get; set; }               // e.g. "Column" or "Table"
        public string ConflictDescription { get; set; }
        public List<ConflictItem> Items { get; set; }
    }

    public class ConflictItem
    {
        public string Name { get; set; }              // e.g. column or table name
        public string TableName { get; set; }         // if type=Column
        public string SuggestedResolution { get; set; }
    }

    public class UnclearElement
    {
        public string Type { get; set; }             // "Column" or "Table"
        public string Name { get; set; }
        public string TableName { get; set; }
        public string Issue { get; set; }
        public string Suggestion { get; set; }
    }

    public class SuggestedRelationship
    {
        public string RelationshipType { get; set; } // "one-to-many", etc.
        public double Confidence { get; set; }
        public TableColumnRef SourceTable { get; set; }
        public TableColumnRef TargetTable { get; set; }
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
        public string version { get; set; }
        public DateTime date { get; set; }
        public string changes { get; set; }
    }
}
