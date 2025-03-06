using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicDashboardCommon.Models.LLM
{
    /// <summary>
    /// Represents options for an adjustable parameter
    /// </summary>
    public class ParameterOptions
    {
        /// <summary>
        /// The current/default value of the parameter
        /// </summary>
        public string DefaultValue { get; set; }

        /// <summary>
        /// User-friendly description of the parameter
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// List of alternative values for the parameter
        /// </summary>
        public List<string> Alternatives { get; set; } = new List<string>();

        /// <summary>
        /// The type of the parameter (date, number, category, etc.)
        /// </summary>
        public string ParameterType { get; set; }
    }
}
