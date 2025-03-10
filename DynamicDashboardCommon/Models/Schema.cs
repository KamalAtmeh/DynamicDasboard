using System.Collections.Generic;

namespace DynamicDashboardCommon.Models
{

        public class SchemaTableDto
        {
            public string TableName { get; set; }
            public string AdminTableName { get; set; }
            public string AdminDescription { get; set; }
            public List<SchemaColumnDto> Columns { get; set; } = new List<SchemaColumnDto>();

            // Add this property for relationship handling
            public List<SchemaRelationshipDto> Relationships { get; set; } = new List<SchemaRelationshipDto>();
        }

        public class SchemaColumnDto
        {
            public string ColumnName { get; set; }
            public string AdminColumnName { get; set; }
            public string DataType { get; set; }
            public bool IsNullable { get; set; }
            public bool IsPrimary { get; set; }
            public bool IsForeignKey { get; set; }
            public string AdminDescription { get; set; }
            public bool IsLookupColumn { get; set; }
        }

        // Add this class for relationship modeling
        public class SchemaRelationshipDto
        {
        public string SourceTable { get; set; }
        public string SourceColumn { get; set; }
        public string TargetTable { get; set; }
        public string TargetColumn { get; set; }
        public string RelationshipType { get; set; }
        public string Description { get; set; }
    }
    }
