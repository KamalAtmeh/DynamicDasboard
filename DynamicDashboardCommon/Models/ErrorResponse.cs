using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicDashboardCommon.Models
{
    public class ExceptionDetails
    {
        public string ErrorCode { get; set; } // A unique code for the error
        public string Message { get; set; }   // A human-readable error message
        public string Details { get; set; }   // Additional details (optional)
        public DateTime Timestamp { get; set; } = DateTime.UtcNow; // Timestamp of the error
    }
}
