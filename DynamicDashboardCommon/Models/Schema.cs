using System.Collections.Generic;

namespace DynamicDashboardCommon.Models
{
    public class SchemaTableDto
    {
        public string TableName { get; set; }
        public string AdminTableName { get; set; }
        public string AdminDescription { get; set; }
        public List<SchemaColumnDto> Columns { get; set; } = new List<SchemaColumnDto>();
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
}