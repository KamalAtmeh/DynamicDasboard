using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicDashboardCommon.Models
{
    public class DirectSqlResult
    {

        public List<Dictionary<string, object>> Data { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }
}
