using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicDashboardCommon.Models
{
    /// <summary>
    /// Represents a log entry for a batch processing job.
    /// </summary>
    public class BatchProcessingLog
    {
        public int BatchId { get; set; }
        public string FileName { get; set; }
        public int TotalQuestions { get; set; }
        public int SuccessCount { get; set; }
        public int? UserId { get; set; }
        public DateTime ProcessedAt { get; set; }
    }
}
