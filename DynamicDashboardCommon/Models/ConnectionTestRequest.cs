﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicDashboardCommon.Models
{
    public class ConnectionTestRequest
    {
        public int DatabaseId { get; set; }
        public string Server { get; set; }
        public string Database { get; set; }
        public string DbType { get; set; }
        public string AuthType { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
