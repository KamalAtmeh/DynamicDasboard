using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicDashboardCommon.Models
{
    public class QueryValidationResult
    {
        /// <summary>
        /// Indicates whether the SQL is valid
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Error message if validation failed
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Objects referenced in the SQL script
        /// </summary>
        public QueryReferencedObjects ReferencedObjects { get; set; }
    }
}
