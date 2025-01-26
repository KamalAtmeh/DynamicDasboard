using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicDashboardCommon.Models
{
    public class QueryComparison
    {
        public string DbSchema { get; set; }
        public string NaturalLanguageQuestion { get; set; }
        public string GptSqlQuery { get; set; }
        public string DeepSeekSqlQuery { get; set; }
        public string DeepSeekChatSqlQuery { get; set; }
        public List<Dictionary<string, object>> GptDataResult { get; set; }
        public List<Dictionary<string, object>> DeepSeekDataResult { get; set; }
        public List<Dictionary<string, object>> DeepSeekChatDataResult { get; set; }
        public bool GptVsDeepSeekChatComparison { get; set; }
        public bool DeepSeekVsDeepSeekChatComparison { get; set; }
    }
}
