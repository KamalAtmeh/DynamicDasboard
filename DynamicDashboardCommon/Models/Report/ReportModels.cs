using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DynamicDashboardCommon.Models
{
    #region Report CRUD DTOs

    /// <summary>
    /// DTO for creating a new report.
    /// </summary>
    public class CreateReportRequest
    {
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
        /// Gets or sets the database ID.
        /// </summary>
        [Required]
        public int DatabaseID { get; set; }

        /// <summary>
        /// Gets or sets the report type.
        /// </summary>
        public ReportTypeEnum ReportType { get; set; } = ReportTypeEnum.Custom;

        /// <summary>
        /// Gets or sets the user creating the report.
        /// </summary>
        public string CreatedBy { get; set; }
    }

    /// <summary>
    /// DTO for updating an existing report.
    /// </summary>
    public class UpdateReportRequest
    {
        /// <summary>
        /// Gets or sets the report ID.
        /// </summary>
        [Required]
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
        /// Gets or sets the report status.
        /// </summary>
        public ReportStatusEnum Status { get; set; }

        /// <summary>
        /// Gets or sets the executive summary.
        /// </summary>
        public string ExecutiveSummary { get; set; }

        /// <summary>
        /// Gets or sets the report configuration.
        /// </summary>
        public string Configuration { get; set; }

        /// <summary>
        /// Gets or sets the user modifying the report.
        /// </summary>
        public string ModifiedBy { get; set; }

        /// <summary>
        /// Gets or sets the sections to update.
        /// </summary>
        public List<ReportSection> Sections { get; set; }
    }

    /// <summary>
    /// DTO for report list item (lightweight).
    /// </summary>
    public class ReportListItemDto
    {
        public int ReportID { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string DatabaseName { get; set; }
        public ReportTypeEnum ReportType { get; set; }
        public ReportStatusEnum Status { get; set; }
        public int SectionCount { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastModifiedAt { get; set; }
    }

    #endregion

    #region Section CRUD DTOs

    /// <summary>
    /// DTO for creating a new section.
    /// </summary>
    public class CreateSectionRequest
    {
        /// <summary>
        /// Gets or sets the parent report ID.
        /// </summary>
        [Required]
        public int ReportID { get; set; }

        /// <summary>
        /// Gets or sets the section title.
        /// </summary>
        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the section description.
        /// </summary>
        [StringLength(500)]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the section type.
        /// </summary>
        public ReportSectionTypeEnum SectionType { get; set; } = ReportSectionTypeEnum.DataTable;

        /// <summary>
        /// Gets or sets the SQL query.
        /// </summary>
        public string QueryText { get; set; }

        /// <summary>
        /// Gets or sets the query intent.
        /// </summary>
        public string QueryIntent { get; set; }

        /// <summary>
        /// Gets or sets the text content (for text sections).
        /// </summary>
        public string TextContent { get; set; }

        /// <summary>
        /// Gets or sets the display order.
        /// </summary>
        public int DisplayOrder { get; set; }
    }

    /// <summary>
    /// DTO for updating a section.
    /// </summary>
    public class UpdateSectionRequest
    {
        /// <summary>
        /// Gets or sets the section ID.
        /// </summary>
        [Required]
        public int SectionID { get; set; }

        /// <summary>
        /// Gets or sets the section title.
        /// </summary>
        [StringLength(200)]
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the section description.
        /// </summary>
        [StringLength(500)]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the SQL query.
        /// </summary>
        public string QueryText { get; set; }

        /// <summary>
        /// Gets or sets the text content.
        /// </summary>
        public string TextContent { get; set; }

        /// <summary>
        /// Gets or sets the column configuration JSON.
        /// </summary>
        public string ColumnConfiguration { get; set; }

        /// <summary>
        /// Gets or sets whether the section is visible.
        /// </summary>
        public bool? IsVisible { get; set; }

        /// <summary>
        /// Gets or sets whether to display as chart.
        /// </summary>
        public bool? IsDisplayedAsChart { get; set; }

        /// <summary>
        /// Gets or sets the chart type.
        /// </summary>
        public string ChartType { get; set; }

        /// <summary>
        /// Gets or sets the display order.
        /// </summary>
        public int? DisplayOrder { get; set; }
    }

    /// <summary>
    /// DTO for reordering sections.
    /// </summary>
    public class ReorderSectionsRequest
    {
        /// <summary>
        /// Gets or sets the report ID.
        /// </summary>
        [Required]
        public int ReportID { get; set; }

        /// <summary>
        /// Gets or sets the section IDs in new order.
        /// </summary>
        [Required]
        public List<int> SectionOrder { get; set; }
    }

    #endregion

    #region AI Generation DTOs

    /// <summary>
    /// Request for AI-powered report generation.
    /// </summary>
    public class GenerateReportRequest
    {
        /// <summary>
        /// Gets or sets the natural language prompt describing the report.
        /// </summary>
        [Required]
        [StringLength(2000)]
        public string Prompt { get; set; }

        /// <summary>
        /// Gets or sets the database ID to query.
        /// </summary>
        [Required]
        public int DatabaseID { get; set; }

        /// <summary>
        /// Gets or sets the report title (optional, can be AI-generated).
        /// </summary>
        [StringLength(200)]
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets whether to generate executive summary.
        /// </summary>
        public bool IncludeExecutiveSummary { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum number of sections to generate.
        /// </summary>
        public int MaxSections { get; set; } = 5;

        /// <summary>
        /// Gets or sets the user creating the report.
        /// </summary>
        public string CreatedBy { get; set; }
    }

    /// <summary>
    /// Response from AI report generation.
    /// </summary>
    public class GenerateReportResponse
    {
        /// <summary>
        /// Gets or sets whether generation was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the error message if generation failed.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the generated report.
        /// </summary>
        public Report Report { get; set; }

        /// <summary>
        /// Gets or sets the AI's explanation of the report structure.
        /// </summary>
        public string Explanation { get; set; }

        /// <summary>
        /// Gets or sets any suggestions for additional sections.
        /// </summary>
        public List<SectionSuggestion> Suggestions { get; set; }
    }

    /// <summary>
    /// Request for adding a single section via AI.
    /// </summary>
    public class GenerateSectionRequest
    {
        /// <summary>
        /// Gets or sets the report ID to add section to.
        /// </summary>
        [Required]
        public int ReportID { get; set; }

        /// <summary>
        /// Gets or sets the natural language description of the section.
        /// </summary>
        [Required]
        [StringLength(1000)]
        public string Prompt { get; set; }

        /// <summary>
        /// Gets or sets the database ID.
        /// </summary>
        [Required]
        public int DatabaseID { get; set; }

        /// <summary>
        /// Gets or sets the preferred section type.
        /// </summary>
        public ReportSectionTypeEnum? PreferredSectionType { get; set; }
    }

    /// <summary>
    /// Response from AI section generation.
    /// </summary>
    public class GenerateSectionResponse
    {
        /// <summary>
        /// Gets or sets whether generation was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the error message if generation failed.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the generated section.
        /// </summary>
        public ReportSection Section { get; set; }

        /// <summary>
        /// Gets or sets the AI's explanation.
        /// </summary>
        public string Explanation { get; set; }
    }

    /// <summary>
    /// Suggestion for additional report sections.
    /// </summary>
    public class SectionSuggestion
    {
        /// <summary>
        /// Gets or sets the suggested section title.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the suggested section description.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the suggested section type.
        /// </summary>
        public ReportSectionTypeEnum SectionType { get; set; }

        /// <summary>
        /// Gets or sets the query intent.
        /// </summary>
        public string QueryIntent { get; set; }
    }

    /// <summary>
    /// Request for AI data explanation.
    /// </summary>
    public class ExplainDataRequest
    {
        /// <summary>
        /// Gets or sets the section ID.
        /// </summary>
        [Required]
        public int SectionID { get; set; }

        /// <summary>
        /// Gets or sets the data to explain.
        /// </summary>
        public List<Dictionary<string, object>> Data { get; set; }

        /// <summary>
        /// Gets or sets the original question/context.
        /// </summary>
        public string Context { get; set; }
    }

    /// <summary>
    /// Response from AI data explanation.
    /// </summary>
    public class ExplainDataResponse
    {
        /// <summary>
        /// Gets or sets whether explanation was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the error message if failed.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the AI-generated explanation.
        /// </summary>
        public string Explanation { get; set; }

        /// <summary>
        /// Gets or sets key insights extracted from the data.
        /// </summary>
        public List<string> KeyInsights { get; set; }

        /// <summary>
        /// Gets or sets any trends identified.
        /// </summary>
        public List<string> Trends { get; set; }
    }

    #endregion

    #region Section Data DTOs

    /// <summary>
    /// Request for executing section query.
    /// </summary>
    public class ExecuteSectionQueryRequest
    {
        /// <summary>
        /// Gets or sets the section ID.
        /// </summary>
        [Required]
        public int SectionID { get; set; }

        /// <summary>
        /// Gets or sets optional filter parameters.
        /// </summary>
        public Dictionary<string, string> Filters { get; set; }

        /// <summary>
        /// Gets or sets the page number (1-based).
        /// </summary>
        public int Page { get; set; } = 1;

        /// <summary>
        /// Gets or sets the page size.
        /// </summary>
        public int PageSize { get; set; } = 25;

        /// <summary>
        /// Gets or sets the sort column.
        /// </summary>
        public string SortColumn { get; set; }

        /// <summary>
        /// Gets or sets the sort direction.
        /// </summary>
        public string SortDirection { get; set; } = "asc";
    }

    /// <summary>
    /// Response from section query execution.
    /// </summary>
    public class ExecuteSectionQueryResponse
    {
        /// <summary>
        /// Gets or sets whether execution was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the error message if failed.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the query results.
        /// </summary>
        public List<Dictionary<string, object>> Data { get; set; }

        /// <summary>
        /// Gets or sets the total row count (before pagination).
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// Gets or sets the column metadata.
        /// </summary>
        public List<ColumnMetadata> Columns { get; set; }

        /// <summary>
        /// Gets or sets the execution time in milliseconds.
        /// </summary>
        public long ExecutionTimeMs { get; set; }
    }

    /// <summary>
    /// Metadata about a result column.
    /// </summary>
    public class ColumnMetadata
    {
        /// <summary>
        /// Gets or sets the column name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the data type.
        /// </summary>
        public string DataType { get; set; }

        /// <summary>
        /// Gets or sets whether the column is nullable.
        /// </summary>
        public bool IsNullable { get; set; }
    }

    #endregion

    #region Export DTOs

    /// <summary>
    /// Request for exporting a report.
    /// </summary>
    public class ExportReportRequest
    {
        /// <summary>
        /// Gets or sets the report ID.
        /// </summary>
        [Required]
        public int ReportID { get; set; }

        /// <summary>
        /// Gets or sets the export format.
        /// </summary>
        [Required]
        public ReportExportFormat Format { get; set; }

        /// <summary>
        /// Gets or sets whether to include all sections or specific ones.
        /// </summary>
        public bool ExportAllSections { get; set; } = true;

        /// <summary>
        /// Gets or sets specific section IDs to export.
        /// </summary>
        public List<int> SectionIDs { get; set; }

        /// <summary>
        /// Gets or sets whether to include executive summary.
        /// </summary>
        public bool IncludeExecutiveSummary { get; set; } = true;

        /// <summary>
        /// Gets or sets the page size for PDF export.
        /// </summary>
        public string PageSize { get; set; } = "A4";

        /// <summary>
        /// Gets or sets whether to include timestamp.
        /// </summary>
        public bool IncludeTimestamp { get; set; } = true;
    }

    /// <summary>
    /// Enum for report export formats.
    /// </summary>
    public enum ReportExportFormat
    {
        /// <summary>PDF format.</summary>
        PDF = 0,

        /// <summary>Excel format.</summary>
        Excel = 1,

        /// <summary>Word/DOCX format.</summary>
        Word = 2,

        /// <summary>HTML format.</summary>
        HTML = 3,

        /// <summary>CSV format.</summary>
        CSV = 4
    }

    #endregion

    #region AI Chat DTOs

    /// <summary>
    /// Message in AI assistant conversation.
    /// </summary>
    public class AIChatMessage
    {
        /// <summary>
        /// Gets or sets the message role (user/assistant).
        /// </summary>
        public string Role { get; set; }

        /// <summary>
        /// Gets or sets the message content.
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// Gets or sets the timestamp.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets any action performed (e.g., "section_added", "report_generated").
        /// </summary>
        public string Action { get; set; }

        /// <summary>
        /// Gets or sets action data (e.g., section ID).
        /// </summary>
        public object ActionData { get; set; }
    }

    /// <summary>
    /// Request for AI chat interaction.
    /// </summary>
    public class AIChatRequest
    {
        /// <summary>
        /// Gets or sets the report ID (if working on existing report).
        /// </summary>
        public int? ReportID { get; set; }

        /// <summary>
        /// Gets or sets the database ID.
        /// </summary>
        [Required]
        public int DatabaseID { get; set; }

        /// <summary>
        /// Gets or sets the user message.
        /// </summary>
        [Required]
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets the conversation history.
        /// </summary>
        public List<AIChatMessage> ConversationHistory { get; set; }
    }

    /// <summary>
    /// Response from AI chat interaction.
    /// </summary>
    public class AIChatResponse
    {
        /// <summary>
        /// Gets or sets whether the request was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the AI response message.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets the action performed.
        /// </summary>
        public string Action { get; set; }

        /// <summary>
        /// Gets or sets action-specific data.
        /// </summary>
        public object ActionData { get; set; }

        /// <summary>
        /// Gets or sets the updated report (if modified).
        /// </summary>
        public Report UpdatedReport { get; set; }

        /// <summary>
        /// Gets or sets any new section added.
        /// </summary>
        public ReportSection NewSection { get; set; }

        /// <summary>
        /// Gets or sets data explanation (if requested).
        /// </summary>
        public ExplainDataResponse DataExplanation { get; set; }
    }

    #endregion
}
