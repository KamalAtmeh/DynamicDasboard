using System;
using System.Collections.Generic;

namespace DynamicDashboardCommon.Models
{
    /// <summary>
    /// Represents a test automation job that processes multiple questions against a database schema.
    /// </summary>
    public class TestAutomationJob
    {
        /// <summary>
        /// The unique identifier for the test job.
        /// </summary>
        public int JobID { get; set; }

        /// <summary>
        /// The name of the file used for the test job.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// The ID of the database schema being tested.
        /// </summary>
        public int DatabaseSchemaID { get; set; }

        /// <summary>
        /// The total number of questions processed in this job.
        /// </summary>
        public int TotalQuestions { get; set; }

        /// <summary>
        /// The number of questions that were successfully processed.
        /// </summary>
        public int SuccessCount { get; set; }

        /// <summary>
        /// The average match score between expected and generated SQL queries (0-1).
        /// </summary>
        public decimal? AverageQueryMatchScore { get; set; }

        /// <summary>
        /// The average match score between expected and generated explanations (0-1).
        /// </summary>
        public decimal? AverageExplanationMatchScore { get; set; }

        /// <summary>
        /// The average match score between expected and actual datasets (0-1).
        /// </summary>
        public decimal? AverageDataMatchScore { get; set; }

        /// <summary>
        /// The identifier of the LLM used for processing.
        /// </summary>
        public string LLMUsed { get; set; }

        /// <summary>
        /// The ID of the user who executed the job, if any.
        /// </summary>
        public int? ExecutedBy { get; set; }

        /// <summary>
        /// The timestamp when the job was executed.
        /// </summary>
        public DateTime ExecutedAt { get; set; }

        /// <summary>
        /// Navigation property for the job details.
        /// </summary>
        public virtual ICollection<TestAutomationDetail> Details { get; set; }
    }
}