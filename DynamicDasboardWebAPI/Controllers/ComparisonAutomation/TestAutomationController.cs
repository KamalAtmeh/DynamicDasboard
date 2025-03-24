// File: DynamicDasboardWebAPI/Controllers/TestAutomation/TestAutomationController.cs

using DynamicDashboardCommon.Models;
using DynamicDasboardWebAPI.Services.TestAutomation;
using Microsoft.AspNetCore.Mvc;
using DynamicDashboardCommon.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.IO;
using DynamicDasboardWebAPI.Services;
using DynamicDashboardCommon.Models.TestAutomation;

namespace DynamicDasboardWebAPI.Controllers
{
    /// <summary>
    /// API Controller for test automation operations.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class TestAutomationController : AppControllerBase
    {
        private readonly TestAutomationService _testService;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestAutomationController"/> class.
        /// </summary>
        /// <param name="testService">The test automation service.</param>
        /// <param name="logsService">Service for logs and exception handling.</param>
        public TestAutomationController(
            TestAutomationService testService,
            ILogsService logsService)
            : base(logsService)
        {
            _testService = testService ?? throw new ArgumentNullException(nameof(testService));
        }

        /// <summary>
        /// Uploads and processes a test file.
        /// </summary>
        /// <param name="file">The test file to upload.</param>
        /// <param name="databaseId">The ID of the database to test against.</param>
        /// <param name="llmProvider">The LLM provider to use for testing.</param>
        /// <returns>The processed file with test results.</returns>
        [HttpPost("upload")]
        public async Task<IActionResult> UploadTestFile(IFormFile file, [FromQuery] int databaseId, [FromQuery] string llmProvider)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file was uploaded.");

            if (file.Length > 10 * 1024 * 1024) // 10 MB limit
                return BadRequest("File size exceeds the maximum limit of 10 MB.");

            if (databaseId <= 0)
                return BadRequest("A valid database ID must be provided.");

            if (string.IsNullOrWhiteSpace(llmProvider))
                return BadRequest("An LLM provider must be specified.");

            try
            {
                using var stream = file.OpenReadStream();
                var result = await _testService.ProcessTestCasesFileAsync(stream, databaseId, llmProvider, GetUserId());

                return File(result, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"TestResults_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, EnumLoggingType.Error.ToString());
            }
        }

        /// <summary>
        /// Gets a template file for test cases.
        /// </summary>
        /// <returns>An Excel template file.</returns>
        [HttpGet("template")]
        public async Task<IActionResult> GetTestTemplate()
        {
            try
            {
                var templateFile = _testService.GenerateTestTemplate();
                return File(templateFile, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "TestTemplate.xlsx");
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, EnumLoggingType.Error.ToString());
            }
        }

        /// <summary>
        /// Gets a sample file with test questions.
        /// </summary>
        /// <returns>An Excel file with sample test questions.</returns>
        [HttpGet("sample-questions")]
        public async Task<IActionResult> GetSampleQuestions()
        {
            try
            {
                var sampleFile = _testService.Generate50TestQuestionsFile();
                return File(sampleFile, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "SampleTestQuestions.xlsx");
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, EnumLoggingType.Error.ToString());
            }
        }

        /// <summary>
        /// Gets recent test jobs.
        /// </summary>
        /// <param name="limit">Maximum number of jobs to retrieve.</param>
        /// <returns>Collection of test jobs.</returns>
        [HttpGet("jobs")]
        public async Task<ActionResult<IEnumerable<TestAutomationJob>>> GetTestJobs([FromQuery] int limit = 10)
        {
            try
            {
                var jobs = await _testService.GetRecentJobsAsync(GetUserId(), limit);
                return Ok(jobs);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, EnumLoggingType.Error.ToString());
            }
        }

        /// <summary>
        /// Gets details of a specific test job.
        /// </summary>
        /// <param name="jobId">The job ID.</param>
        /// <param name="pageNumber">The page number.</param>
        /// <param name="pageSize">The page size.</param>
        /// <returns>Test job details with pagination.</returns>
        [HttpGet("jobs/{jobId}")]
        public async Task<ActionResult<IEnumerable<TestAutomationDetail>>> GetTestJobDetails(
            int jobId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var details = await _testService.GetJobDetailsPaginatedAsync(jobId, pageNumber, pageSize);
                return Ok(details);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, EnumLoggingType.Error.ToString());
            }
        }

        /// <summary>
        /// Gets comparison data for a specific test detail.
        /// </summary>
        /// <param name="detailId">The test detail ID.</param>
        /// <returns>Dataset comparison data.</returns>
        [HttpGet("comparison/{detailId}")]
        public async Task<IActionResult> GetDatasetComparison(int detailId)
        {
            try
            {

                DatasetComparisonResult objcomparison = await _testService.GetDatasetComparisonAsync(detailId);

                
                bool hasExpectedData = objcomparison.Expected != null && objcomparison.Expected.Any();
                bool hasActualData = objcomparison.Actual != null && objcomparison.Actual.Any();

                return Ok(objcomparison);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, EnumLoggingType.Error.ToString(),
                    $"Error retrieving dataset comparison for detail ID {detailId}");
            }
        }

        // Add this endpoint to TestAutomationController
        [HttpPost("import-json")]
        public async Task<IActionResult> ImportJsonTestCases([FromBody] TestCasesImportRequest request)
        {
            if (request == null || request.TestCases == null || !request.TestCases.Any())
                return BadRequest("No test cases provided");

            if (request.DatabaseId <= 0)
                return BadRequest("A valid database ID must be provided");

            try
            {
                // Convert the JSON test cases to Excel format
                var excelBytes = _testService.ConvertJsonToExcelTemplate(request);

                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "GeneratedTestCases.xlsx");
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, EnumLoggingType.Error.ToString());
            }
        }
    }
}