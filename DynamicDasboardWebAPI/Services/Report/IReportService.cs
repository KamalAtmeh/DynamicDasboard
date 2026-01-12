using DynamicDashboardCommon.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DynamicDasboardWebAPI.Services.Report
{
    /// <summary>
    /// Service interface for Report business logic operations.
    /// </summary>
    public interface IReportService
    {
        #region Report CRUD Operations

        /// <summary>
        /// Gets all reports with optional filtering.
        /// </summary>
        /// <param name="databaseId">Optional database ID filter.</param>
        /// <param name="status">Optional status filter.</param>
        /// <param name="createdBy">Optional creator filter.</param>
        /// <returns>List of report summaries.</returns>
        Task<List<ReportListItemDto>> GetAllReportsAsync(int? databaseId = null, ReportStatusEnum? status = null, string createdBy = null);

        /// <summary>
        /// Gets a report by ID with all sections and data.
        /// </summary>
        /// <param name="reportId">The report ID.</param>
        /// <returns>The complete report.</returns>
        Task<DynamicDashboardCommon.Models.Report> GetReportByIdAsync(int reportId);

        /// <summary>
        /// Creates a new report manually (without AI).
        /// </summary>
        /// <param name="request">The create report request.</param>
        /// <returns>The created report.</returns>
        Task<DynamicDashboardCommon.Models.Report> CreateReportAsync(CreateReportRequest request);

        /// <summary>
        /// Updates an existing report.
        /// </summary>
        /// <param name="request">The update report request.</param>
        /// <returns>The updated report.</returns>
        Task<DynamicDashboardCommon.Models.Report> UpdateReportAsync(UpdateReportRequest request);

        /// <summary>
        /// Deletes a report.
        /// </summary>
        /// <param name="reportId">The report ID.</param>
        /// <returns>True if deleted successfully.</returns>
        Task<bool> DeleteReportAsync(int reportId);

        #endregion

        #region Section Operations

        /// <summary>
        /// Adds a new section to a report manually.
        /// </summary>
        /// <param name="request">The create section request.</param>
        /// <returns>The created section.</returns>
        Task<ReportSection> AddSectionAsync(CreateSectionRequest request);

        /// <summary>
        /// Updates an existing section.
        /// </summary>
        /// <param name="request">The update section request.</param>
        /// <returns>The updated section.</returns>
        Task<ReportSection> UpdateSectionAsync(UpdateSectionRequest request);

        /// <summary>
        /// Deletes a section from a report.
        /// </summary>
        /// <param name="sectionId">The section ID.</param>
        /// <returns>True if deleted successfully.</returns>
        Task<bool> DeleteSectionAsync(int sectionId);

        /// <summary>
        /// Reorders sections within a report.
        /// </summary>
        /// <param name="request">The reorder request.</param>
        /// <returns>True if reordered successfully.</returns>
        Task<bool> ReorderSectionsAsync(ReorderSectionsRequest request);

        /// <summary>
        /// Toggles section visibility.
        /// </summary>
        /// <param name="sectionId">The section ID.</param>
        /// <param name="isVisible">Visibility state.</param>
        /// <returns>The updated section.</returns>
        Task<ReportSection> ToggleSectionVisibilityAsync(int sectionId, bool isVisible);

        /// <summary>
        /// Converts a section between table and chart display.
        /// </summary>
        /// <param name="sectionId">The section ID.</param>
        /// <param name="displayAsChart">Whether to display as chart.</param>
        /// <param name="chartType">The chart type if displaying as chart.</param>
        /// <returns>The updated section.</returns>
        Task<ReportSection> ToggleSectionChartModeAsync(int sectionId, bool displayAsChart, string chartType = "bar");

        #endregion

        #region AI Generation Operations

        /// <summary>
        /// Generates a complete report using AI from a natural language prompt.
        /// </summary>
        /// <param name="request">The generation request.</param>
        /// <returns>The generated report response.</returns>
        Task<GenerateReportResponse> GenerateReportAsync(GenerateReportRequest request);

        /// <summary>
        /// Adds a new section to an existing report using AI.
        /// </summary>
        /// <param name="request">The section generation request.</param>
        /// <returns>The generated section response.</returns>
        Task<GenerateSectionResponse> GenerateSectionAsync(GenerateSectionRequest request);

        /// <summary>
        /// Regenerates the executive summary for a report.
        /// </summary>
        /// <param name="reportId">The report ID.</param>
        /// <returns>The updated report with new summary.</returns>
        Task<DynamicDashboardCommon.Models.Report> RegenerateExecutiveSummaryAsync(int reportId);

        /// <summary>
        /// Gets AI explanation of section data.
        /// </summary>
        /// <param name="request">The explain data request.</param>
        /// <returns>The explanation response.</returns>
        Task<ExplainDataResponse> ExplainDataAsync(ExplainDataRequest request);

        /// <summary>
        /// Processes an AI chat message for report building.
        /// </summary>
        /// <param name="request">The chat request.</param>
        /// <returns>The AI response with any actions taken.</returns>
        Task<AIChatResponse> ProcessChatMessageAsync(AIChatRequest request);

        #endregion

        #region Section Data Operations

        /// <summary>
        /// Executes a section's query and returns data.
        /// </summary>
        /// <param name="request">The execution request.</param>
        /// <returns>The query results.</returns>
        Task<ExecuteSectionQueryResponse> ExecuteSectionQueryAsync(ExecuteSectionQueryRequest request);

        /// <summary>
        /// Executes a section's query with full data (no pagination).
        /// </summary>
        /// <param name="sectionId">The section ID.</param>
        /// <returns>All query results.</returns>
        Task<ExecuteSectionQueryResponse> ExecuteSectionQueryFullAsync(int sectionId);

        /// <summary>
        /// Tests a SQL query without saving.
        /// </summary>
        /// <param name="sql">The SQL query.</param>
        /// <param name="databaseId">The database ID.</param>
        /// <returns>Query results or error.</returns>
        Task<ExecuteSectionQueryResponse> TestQueryAsync(string sql, int databaseId);

        /// <summary>
        /// Gets column metadata for a section's query.
        /// </summary>
        /// <param name="sectionId">The section ID.</param>
        /// <returns>List of column metadata.</returns>
        Task<List<ColumnMetadata>> GetSectionColumnsAsync(int sectionId);

        #endregion

        #region Export Operations

        /// <summary>
        /// Exports a report to the specified format.
        /// </summary>
        /// <param name="request">The export request.</param>
        /// <returns>The exported file bytes.</returns>
        Task<byte[]> ExportReportAsync(ExportReportRequest request);

        /// <summary>
        /// Exports a single section's data.
        /// </summary>
        /// <param name="sectionId">The section ID.</param>
        /// <param name="format">The export format.</param>
        /// <returns>The exported file bytes.</returns>
        Task<byte[]> ExportSectionDataAsync(int sectionId, ReportExportFormat format);

        #endregion
    }
}
