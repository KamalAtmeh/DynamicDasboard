using DynamicDashboardCommon.Models;
using DynamicDasboardWebAPI.Services.Report;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DynamicDasboardWebAPI.Controllers
{
    /// <summary>
    /// API Controller for AI Report Builder operations.
    /// Handles report CRUD, section management, AI generation, and data operations.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ReportController : ControllerBase
    {
        private readonly IReportService _reportService;

        /// <summary>
        /// Initializes a new instance of the ReportController.
        /// </summary>
        /// <param name="reportService">The report service.</param>
        public ReportController(IReportService reportService)
        {
            _reportService = reportService ?? throw new ArgumentNullException(nameof(reportService));
        }

        #region Report CRUD Endpoints

        /// <summary>
        /// Gets all reports with optional filtering.
        /// </summary>
        /// <param name="databaseId">Optional database ID filter.</param>
        /// <param name="status">Optional status filter (0=Draft, 1=Published, 2=Archived).</param>
        /// <param name="createdBy">Optional creator filter.</param>
        /// <returns>List of reports.</returns>
        [HttpGet]
        public async Task<ActionResult<List<ReportListItemDto>>> GetAllReports(
            [FromQuery] int? databaseId = null,
            [FromQuery] int? status = null,
            [FromQuery] string createdBy = null)
        {
            try
            {
                ReportStatusEnum? statusEnum = status.HasValue ? (ReportStatusEnum)status.Value : null;
                var reports = await _reportService.GetAllReportsAsync(databaseId, statusEnum, createdBy);
                return Ok(reports);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Gets a report by ID with all sections.
        /// </summary>
        /// <param name="id">The report ID.</param>
        /// <returns>The complete report.</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<DynamicDashboardCommon.Models.Report>> GetReportById(int id)
        {
            try
            {
                var report = await _reportService.GetReportByIdAsync(id);
                if (report == null)
                    return NotFound(new { error = $"Report with ID {id} not found." });

                return Ok(report);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Creates a new report manually (without AI).
        /// </summary>
        /// <param name="request">The create report request.</param>
        /// <returns>The created report.</returns>
        [HttpPost]
        public async Task<ActionResult<DynamicDashboardCommon.Models.Report>> CreateReport([FromBody] CreateReportRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var report = await _reportService.CreateReportAsync(request);
                return CreatedAtAction(nameof(GetReportById), new { id = report.ReportID }, report);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Updates an existing report.
        /// </summary>
        /// <param name="id">The report ID.</param>
        /// <param name="request">The update request.</param>
        /// <returns>The updated report.</returns>
        [HttpPut("{id}")]
        public async Task<ActionResult<DynamicDashboardCommon.Models.Report>> UpdateReport(int id, [FromBody] UpdateReportRequest request)
        {
            try
            {
                if (id != request.ReportID)
                    return BadRequest(new { error = "Report ID mismatch." });

                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var report = await _reportService.UpdateReportAsync(request);
                return Ok(report);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Deletes a report.
        /// </summary>
        /// <param name="id">The report ID.</param>
        /// <returns>Success status.</returns>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteReport(int id)
        {
            try
            {
                var result = await _reportService.DeleteReportAsync(id);
                if (!result)
                    return NotFound(new { error = $"Report with ID {id} not found." });

                return Ok(new { success = true, message = "Report deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        #endregion

        #region Section CRUD Endpoints

        /// <summary>
        /// Gets all sections for a report.
        /// </summary>
        /// <param name="reportId">The report ID.</param>
        /// <returns>List of sections.</returns>
        [HttpGet("{reportId}/sections")]
        public async Task<ActionResult<List<ReportSection>>> GetReportSections(int reportId)
        {
            try
            {
                var report = await _reportService.GetReportByIdAsync(reportId);
                if (report == null)
                    return NotFound(new { error = $"Report with ID {reportId} not found." });

                return Ok(report.Sections);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Adds a new section to a report.
        /// </summary>
        /// <param name="reportId">The report ID.</param>
        /// <param name="request">The create section request.</param>
        /// <returns>The created section.</returns>
        [HttpPost("{reportId}/sections")]
        public async Task<ActionResult<ReportSection>> AddSection(int reportId, [FromBody] CreateSectionRequest request)
        {
            try
            {
                if (reportId != request.ReportID)
                    return BadRequest(new { error = "Report ID mismatch." });

                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var section = await _reportService.AddSectionAsync(request);
                return CreatedAtAction(nameof(GetSectionById), new { reportId = reportId, sectionId = section.SectionID }, section);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Gets a section by ID.
        /// </summary>
        /// <param name="reportId">The report ID.</param>
        /// <param name="sectionId">The section ID.</param>
        /// <returns>The section.</returns>
        [HttpGet("{reportId}/sections/{sectionId}")]
        public async Task<ActionResult<ReportSection>> GetSectionById(int reportId, int sectionId)
        {
            try
            {
                var report = await _reportService.GetReportByIdAsync(reportId);
                if (report == null)
                    return NotFound(new { error = $"Report with ID {reportId} not found." });

                var section = report.Sections?.FirstOrDefault(s => s.SectionID == sectionId);
                if (section == null)
                    return NotFound(new { error = $"Section with ID {sectionId} not found." });

                return Ok(section);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Updates a section.
        /// </summary>
        /// <param name="reportId">The report ID.</param>
        /// <param name="sectionId">The section ID.</param>
        /// <param name="request">The update request.</param>
        /// <returns>The updated section.</returns>
        [HttpPut("{reportId}/sections/{sectionId}")]
        public async Task<ActionResult<ReportSection>> UpdateSection(int reportId, int sectionId, [FromBody] UpdateSectionRequest request)
        {
            try
            {
                if (sectionId != request.SectionID)
                    return BadRequest(new { error = "Section ID mismatch." });

                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var section = await _reportService.UpdateSectionAsync(request);
                return Ok(section);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Deletes a section.
        /// </summary>
        /// <param name="reportId">The report ID.</param>
        /// <param name="sectionId">The section ID.</param>
        /// <returns>Success status.</returns>
        [HttpDelete("{reportId}/sections/{sectionId}")]
        public async Task<ActionResult> DeleteSection(int reportId, int sectionId)
        {
            try
            {
                var result = await _reportService.DeleteSectionAsync(sectionId);
                if (!result)
                    return NotFound(new { error = $"Section with ID {sectionId} not found." });

                return Ok(new { success = true, message = "Section deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Reorders sections within a report.
        /// </summary>
        /// <param name="reportId">The report ID.</param>
        /// <param name="request">The reorder request.</param>
        /// <returns>Success status.</returns>
        [HttpPost("{reportId}/sections/reorder")]
        public async Task<ActionResult> ReorderSections(int reportId, [FromBody] ReorderSectionsRequest request)
        {
            try
            {
                if (reportId != request.ReportID)
                    return BadRequest(new { error = "Report ID mismatch." });

                var result = await _reportService.ReorderSectionsAsync(request);
                if (!result)
                    return BadRequest(new { error = "Failed to reorder sections." });

                return Ok(new { success = true, message = "Sections reordered successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Toggles section visibility.
        /// </summary>
        /// <param name="reportId">The report ID.</param>
        /// <param name="sectionId">The section ID.</param>
        /// <param name="isVisible">Visibility state.</param>
        /// <returns>The updated section.</returns>
        [HttpPatch("{reportId}/sections/{sectionId}/visibility")]
        public async Task<ActionResult<ReportSection>> ToggleSectionVisibility(int reportId, int sectionId, [FromQuery] bool isVisible)
        {
            try
            {
                var section = await _reportService.ToggleSectionVisibilityAsync(sectionId, isVisible);
                return Ok(section);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Toggles section between table and chart display.
        /// </summary>
        /// <param name="reportId">The report ID.</param>
        /// <param name="sectionId">The section ID.</param>
        /// <param name="displayAsChart">Whether to display as chart.</param>
        /// <param name="chartType">The chart type.</param>
        /// <returns>The updated section.</returns>
        [HttpPatch("{reportId}/sections/{sectionId}/chart-mode")]
        public async Task<ActionResult<ReportSection>> ToggleSectionChartMode(
            int reportId, 
            int sectionId, 
            [FromQuery] bool displayAsChart,
            [FromQuery] string chartType = "bar")
        {
            try
            {
                var section = await _reportService.ToggleSectionChartModeAsync(sectionId, displayAsChart, chartType);
                return Ok(section);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        #endregion

        #region AI Generation Endpoints

        /// <summary>
        /// Generates a complete report using AI from a natural language prompt.
        /// </summary>
        /// <param name="request">The generation request.</param>
        /// <returns>The generated report response.</returns>
        [HttpPost("generate")]
        public async Task<ActionResult<GenerateReportResponse>> GenerateReport([FromBody] GenerateReportRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var response = await _reportService.GenerateReportAsync(request);

                if (!response.Success)
                    return BadRequest(response);

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new GenerateReportResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }
        }

        /// <summary>
        /// Generates a new section using AI.
        /// </summary>
        /// <param name="reportId">The report ID.</param>
        /// <param name="request">The section generation request.</param>
        /// <returns>The generated section response.</returns>
        [HttpPost("{reportId}/sections/generate")]
        public async Task<ActionResult<GenerateSectionResponse>> GenerateSection(int reportId, [FromBody] GenerateSectionRequest request)
        {
            try
            {
                if (reportId != request.ReportID)
                    return BadRequest(new { error = "Report ID mismatch." });

                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var response = await _reportService.GenerateSectionAsync(request);

                if (!response.Success)
                    return BadRequest(response);

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new GenerateSectionResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }
        }

        /// <summary>
        /// Regenerates the executive summary for a report.
        /// </summary>
        /// <param name="reportId">The report ID.</param>
        /// <returns>The updated report.</returns>
        [HttpPost("{reportId}/regenerate-summary")]
        public async Task<ActionResult<DynamicDashboardCommon.Models.Report>> RegenerateExecutiveSummary(int reportId)
        {
            try
            {
                var report = await _reportService.RegenerateExecutiveSummaryAsync(reportId);
                return Ok(report);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Gets AI explanation of section data.
        /// </summary>
        /// <param name="reportId">The report ID.</param>
        /// <param name="sectionId">The section ID.</param>
        /// <param name="request">The explain data request.</param>
        /// <returns>The explanation response.</returns>
        [HttpPost("{reportId}/sections/{sectionId}/explain")]
        public async Task<ActionResult<ExplainDataResponse>> ExplainData(int reportId, int sectionId, [FromBody] ExplainDataRequest request)
        {
            try
            {
                if (sectionId != request.SectionID)
                    return BadRequest(new { error = "Section ID mismatch." });

                var response = await _reportService.ExplainDataAsync(request);

                if (!response.Success)
                    return BadRequest(response);

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ExplainDataResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }
        }

        /// <summary>
        /// Processes an AI chat message for report building.
        /// </summary>
        /// <param name="request">The chat request.</param>
        /// <returns>The AI response.</returns>
        [HttpPost("chat")]
        public async Task<ActionResult<AIChatResponse>> Chat([FromBody] AIChatRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var response = await _reportService.ProcessChatMessageAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new AIChatResponse
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        #endregion

        #region Section Data Endpoints

        /// <summary>
        /// Executes a section's query and returns paginated data.
        /// </summary>
        /// <param name="reportId">The report ID.</param>
        /// <param name="sectionId">The section ID.</param>
        /// <param name="page">Page number (1-based).</param>
        /// <param name="pageSize">Page size.</param>
        /// <param name="sortColumn">Optional sort column.</param>
        /// <param name="sortDirection">Sort direction (asc/desc).</param>
        /// <returns>The query results.</returns>
        [HttpGet("{reportId}/sections/{sectionId}/data")]
        public async Task<ActionResult<ExecuteSectionQueryResponse>> GetSectionData(
            int reportId,
            int sectionId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 25,
            [FromQuery] string sortColumn = null,
            [FromQuery] string sortDirection = "asc")
        {
            try
            {
                var request = new ExecuteSectionQueryRequest
                {
                    SectionID = sectionId,
                    Page = page,
                    PageSize = pageSize,
                    SortColumn = sortColumn,
                    SortDirection = sortDirection
                };

                var response = await _reportService.ExecuteSectionQueryAsync(request);

                if (!response.Success)
                    return BadRequest(response);

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ExecuteSectionQueryResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }
        }

        /// <summary>
        /// Executes a section's query and returns all data (no pagination).
        /// </summary>
        /// <param name="reportId">The report ID.</param>
        /// <param name="sectionId">The section ID.</param>
        /// <returns>All query results.</returns>
        [HttpGet("{reportId}/sections/{sectionId}/data/all")]
        public async Task<ActionResult<ExecuteSectionQueryResponse>> GetSectionDataFull(int reportId, int sectionId)
        {
            try
            {
                var response = await _reportService.ExecuteSectionQueryFullAsync(sectionId);

                if (!response.Success)
                    return BadRequest(response);

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ExecuteSectionQueryResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }
        }

        /// <summary>
        /// Tests a SQL query without saving.
        /// </summary>
        /// <param name="sql">The SQL query.</param>
        /// <param name="databaseId">The database ID.</param>
        /// <returns>Query results or error.</returns>
        [HttpPost("test-query")]
        public async Task<ActionResult<ExecuteSectionQueryResponse>> TestQuery([FromBody] TestQueryRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Sql))
                    return BadRequest(new { error = "SQL query is required." });

                var response = await _reportService.TestQueryAsync(request.Sql, request.DatabaseId);

                if (!response.Success)
                    return BadRequest(response);

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ExecuteSectionQueryResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }
        }

        /// <summary>
        /// Gets column metadata for a section's query.
        /// </summary>
        /// <param name="reportId">The report ID.</param>
        /// <param name="sectionId">The section ID.</param>
        /// <returns>List of column metadata.</returns>
        [HttpGet("{reportId}/sections/{sectionId}/columns")]
        public async Task<ActionResult<List<ColumnMetadata>>> GetSectionColumns(int reportId, int sectionId)
        {
            try
            {
                var columns = await _reportService.GetSectionColumnsAsync(sectionId);
                return Ok(columns);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        #endregion

        #region Export Endpoints

        /// <summary>
        /// Exports a report to the specified format.
        /// </summary>
        /// <param name="reportId">The report ID.</param>
        /// <param name="request">The export request.</param>
        /// <returns>The exported file.</returns>
        [HttpPost("{reportId}/export")]
        public async Task<ActionResult> ExportReport(int reportId, [FromBody] ExportReportRequest request)
        {
            try
            {
                if (reportId != request.ReportID)
                    return BadRequest(new { error = "Report ID mismatch." });

                var fileBytes = await _reportService.ExportReportAsync(request);

                var contentType = GetContentType(request.Format);
                var fileName = $"Report_{reportId}_{DateTime.Now:yyyyMMdd_HHmmss}.{GetFileExtension(request.Format)}";

                return File(fileBytes, contentType, fileName);
            }
            catch (NotImplementedException ex)
            {
                return StatusCode(501, new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Exports a single section's data.
        /// </summary>
        /// <param name="reportId">The report ID.</param>
        /// <param name="sectionId">The section ID.</param>
        /// <param name="format">The export format (0=PDF, 1=Excel, 2=Word, 3=HTML, 4=CSV).</param>
        /// <returns>The exported file.</returns>
        [HttpGet("{reportId}/sections/{sectionId}/export")]
        public async Task<ActionResult> ExportSectionData(int reportId, int sectionId, [FromQuery] int format = 1)
        {
            try
            {
                var exportFormat = (ReportExportFormat)format;
                var fileBytes = await _reportService.ExportSectionDataAsync(sectionId, exportFormat);

                var contentType = GetContentType(exportFormat);
                var fileName = $"Section_{sectionId}_{DateTime.Now:yyyyMMdd_HHmmss}.{GetFileExtension(exportFormat)}";

                return File(fileBytes, contentType, fileName);
            }
            catch (NotImplementedException ex)
            {
                return StatusCode(501, new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        #endregion

        #region Private Helper Methods

        private string GetContentType(ReportExportFormat format)
        {
            return format switch
            {
                ReportExportFormat.PDF => "application/pdf",
                ReportExportFormat.Excel => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ReportExportFormat.Word => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ReportExportFormat.HTML => "text/html",
                ReportExportFormat.CSV => "text/csv",
                _ => "application/octet-stream"
            };
        }

        private string GetFileExtension(ReportExportFormat format)
        {
            return format switch
            {
                ReportExportFormat.PDF => "pdf",
                ReportExportFormat.Excel => "xlsx",
                ReportExportFormat.Word => "docx",
                ReportExportFormat.HTML => "html",
                ReportExportFormat.CSV => "csv",
                _ => "bin"
            };
        }

        #endregion
    }

    #region Request DTOs

    /// <summary>
    /// Request model for testing SQL queries.
    /// </summary>
    public class TestQueryRequest
    {
        /// <summary>
        /// Gets or sets the SQL query to test.
        /// </summary>
        public string Sql { get; set; }

        /// <summary>
        /// Gets or sets the database ID.
        /// </summary>
        public int DatabaseId { get; set; }
    }

    #endregion
}
