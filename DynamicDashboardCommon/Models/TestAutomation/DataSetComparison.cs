using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicDashboardCommon.Models.TestAutomation
{
    public class DatasetComparisonResult
    {
        public List<Dictionary<string, object>> Expected { get; set; }
        public List<Dictionary<string, object>> Actual { get; set; }
    }
}
