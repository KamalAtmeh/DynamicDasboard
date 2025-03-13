using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicDashboardCommon.Models
{
    public class QueryReferencedObjects
    {
        /// <summary>
        /// Tables referenced in the script
        /// </summary>
        public HashSet<string> Tables { get; set; }

        /// <summary>
        /// Columns referenced by table
        /// </summary>
        public Dictionary<string, HashSet<string>> TableColumns { get; set; }

        /// <summary>
        /// Relations referenced in join conditions
        /// </summary>
        public List<(string SourceTable, string SourceColumn, string TargetTable, string TargetColumn)> Relations { get; set; }
    }
}