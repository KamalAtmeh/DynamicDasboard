using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DynamicDashboardCommon.Models
{
    /// <summary>
    /// Represents an AI-generated report entity.
    /// A report contains multiple sections, each with its own data query and visualization.
    /// </summary>
    public class Report
    {
        #region Primary Properties

        /// <summary>
        /// Gets or sets the unique identifier for the report.
        /// </summary>
        [Key]
        public int ReportID { get; set; }

        /// <summary>
        /// Gets or sets the report title.
        /// </summary>
        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the report description.
        /// </summary>
        [StringLength(1000)]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the database ID this report is associated with.
        /// </summary>
        [Required]
        public int DatabaseID { get; set; }

        /// <summary>
        /// Gets or sets the report type.
        /// </summary>
        public ReportTypeEnum ReportType { get; set; } = ReportTypeEnum.Custom;

        /// <summary>
        /// Gets or sets the report status.
        /// </summary>
        public ReportStatusEnum Status { get; set; } = ReportStatusEnum.Draft;

        #endregion

        #region AI Generation Properties

        /// <summary>
        /// Gets or sets the original natural language prompt used to generate this report.
        /// </summary>
        [StringLength(2000)]
        public string GeneratedPrompt { get; set; }

        /// <summary>
        /// Gets or sets the AI-generated executive summary for this report.
        /// </summary>
        public string ExecutiveSummary { get; set; }

        /// <summary>
        /// Gets or sets the LLM provider used to generate this report.
        /// </summary>
        [StringLength(50)]
        public string LLMProvider { get; set; }

        #endregion

        #region Configuration Properties

        /// <summary>
        /// Gets or sets the report configuration as JSON.
        /// Includes styling, branding, and layout options.
        /// </summary>
        public string Configuration { get; set; }

        /// <summary>
        /// Gets or sets whether this report is a template.
        /// </summary>
        public bool IsTemplate { get; set; } = false;

        /// <summary>
        /// Gets or sets whether this report is publicly visible in the marketplace.
        /// </summary>
        public bool IsPublic { get; set; } = false;

        #endregion

        #region Audit Properties

        /// <summary>
        /// Gets or sets the user who created this report.
        /// </summary>
        [StringLength(100)]
        public string CreatedBy { get; set; }

        /// <summary>
        /// Gets or sets the creation timestamp.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the user who last modified this report.
        /// </summary>
        [StringLength(100)]
        public string LastModifiedBy { get; set; }

        /// <summary>
        /// Gets or sets the last modification timestamp.
        /// </summary>
        public DateTime LastModifiedAt { get; set; } = DateTime.UtcNow;

        #endregion

        #region Navigation Properties

        /// <summary>
        /// Gets or sets the collection of sections in this report.
        /// </summary>
        public virtual ICollection<ReportSection> Sections { get; set; } = new List<ReportSection>();

        /// <summary>
        /// Gets or sets the associated database (navigation property).
        /// </summary>
        [ForeignKey("DatabaseID")]
        public virtual Database Database { get; set; }

        #endregion

        #region Computed Properties

        /// <summary>
        /// Gets the total number of sections in this report.
        /// </summary>
        [NotMapped]
        public int SectionCount => Sections?.Count ?? 0;

        /// <summary>
        /// Gets the database name (for display purposes).
        /// </summary>
        [NotMapped]
        public string DatabaseName { get; set; }

        /// <summary>
        /// Gets the status display name.
        /// </summary>
        [NotMapped]
        public string StatusName => Status.ToString();

        /// <summary>
        /// Gets the report type display name.
        /// </summary>
        [NotMapped]
        public string ReportTypeName => ReportType.ToString();

        #endregion
    }

    /// <summary>
    /// Enum representing report types.
    /// </summary>
    public enum ReportTypeEnum
    {
        /// <summary>Custom report created by user.</summary>
        Custom = 0,

        /// <summary>Sales performance report.</summary>
        Sales = 1,

        /// <summary>Financial report.</summary>
        Financial = 2,

        /// <summary>Human resources report.</summary>
        HR = 3,

        /// <summary>Operations report.</summary>
        Operations = 4,

        /// <summary>Marketing report.</summary>
        Marketing = 5,

        /// <summary>Inventory report.</summary>
        Inventory = 6,

        /// <summary>Customer analytics report.</summary>
        Customer = 7
    }

    /// <summary>
    /// Enum representing report status.
    /// </summary>
    public enum ReportStatusEnum
    {
        /// <summary>Report is in draft state.</summary>
        Draft = 0,

        /// <summary>Report is published and active.</summary>
        Published = 1,

        /// <summary>Report is archived.</summary>
        Archived = 2
    }
}
