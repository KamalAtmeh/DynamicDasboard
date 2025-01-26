using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicDashboardCommon.Models
{
    public class TableMetadata
    {
        public string DBTableName { get; set; }

        public string AdminTableName { get; set; }
        public string DBColumnName { get; set; }

        public string AdmincnName { get; set; }
        public string DataType { get; set; }
        public bool IsNullable { get; set; }
        public bool IsPrimary { get; set; }

        public bool IsFK { get; set; }

        public bool IsUnique { get; set; }
        public string DBDescription { get; set; }
    }
}
