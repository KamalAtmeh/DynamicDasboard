using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicDashboardCommon.Models
{

    /// <summary>
    /// Represents a request to query a specific schema with a given question.
    /// </summary>
    public class QueryRequest
    {
        /// <summary>
        /// Gets or sets the schema to be queried.
        /// </summary>
        public string Schema { get; set; }

        /// <summary>
        /// Gets or sets the question to be asked against the schema.
        /// </summary>
        public string Question { get; set; }
    }
}
