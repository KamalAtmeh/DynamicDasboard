using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicDashboardCommon.Models
{
    public class Column
    {
        public int ColumnID { get; set; }
        public int TableID { get; set; } // Foreign key to the Table table
        public string DBColumnName { get; set; } // Actual column name in the database
        public string AdminColumnName { get; set; } // User-friendly name for the column
        public string DataType { get; set; } // Data type of the column (e.g., varchar, int)
        public bool IsNullable { get; set; } // Whether the column allows null values
        public string AdminDescription { get; set; } // Description provided by the admin
    }
}
