using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicDashboardCommon.Models
{
    /// <summary>
    /// Custom exception for database operations
    /// </summary>
    public class DatabaseException : Exception
    {
        public DatabaseException(string message, Exception innerException = null)
            : base(message, innerException)
        {
        }
    }
}