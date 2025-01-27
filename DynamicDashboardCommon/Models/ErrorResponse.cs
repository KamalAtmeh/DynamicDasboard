using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicDashboardCommon.Models
{
    /// <summary>
    /// Represents the details of an exception that occurs within the application.
    /// </summary>
    public class ExceptionDetails
    {
        /// <summary>
        /// Gets or sets a unique code for the error.
        /// </summary>
        public string ErrorCode { get; set; } // A unique code for the error

        /// <summary>
        /// Gets or sets a human-readable error message.
        /// </summary>
        public string Message { get; set; }   // A human-readable error message

        /// <summary>
        /// Gets or sets additional details about the error (optional).
        /// </summary>
        public string Details { get; set; }   // Additional details (optional)

        /// <summary>
        /// Gets or sets the timestamp of when the error occurred.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow; // Timestamp of the error
    }
}
