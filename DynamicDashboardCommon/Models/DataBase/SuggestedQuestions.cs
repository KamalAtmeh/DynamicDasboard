using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DynamicDashboardCommon.Models
{
    public class SuggestedQuestions
    {
        /// <summary>
        /// List of example questions for the database.
        /// </summary>
        [JsonPropertyName("questions")]
        public List<string> Questions { get; set; } = new List<string>();

        /// <summary>
        /// Optional category grouping for the questions.
        /// </summary>
        [JsonPropertyName("categories")]
        public Dictionary<string, List<string>> Categories { get; set; } = new Dictionary<string, List<string>>();
    }
}
