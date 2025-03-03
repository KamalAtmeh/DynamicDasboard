using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicDashboardCommon.Models
{
    public class DirectSqlRequest
    {

        public string SqlQuery { get; set; }
        public string DbType { get; set; }
        public int? UserId { get; set; }
        public int DatabaseId { get; set; }

    }
}
