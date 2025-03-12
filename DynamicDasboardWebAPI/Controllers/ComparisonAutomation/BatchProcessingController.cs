using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Threading.Tasks;
using DynamicDasboardWebAPI.Services;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices.ComTypes;
using DynamicDashboardCommon.Enums;

namespace DynamicDasboardWebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BatchProcessingController : AppControllerBase
    {
        private readonly BatchProcessingService _batchProcessingService;

        public BatchProcessingController(
        BatchProcessingService batchProcessingService,
        ILogsService logsService)
        : base(logsService)
        {
            _batchProcessingService = batchProcessingService;
        }

        [HttpPost("process")]
        public async Task<IActionResult> ProcessExcelFile([FromForm] IFormFile file, [FromForm] string dbType)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            try
            {
                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    stream.Position = 0;

                    var result = await _batchProcessingService.ProcessQuestionsFile(stream, dbType);
                    return File(result, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Processed_Questions.xlsx");
                }
            }
            catch (Exception ex)
            {
               return await HandleExceptionAsync(ex, EnumLoggingType.Error.ToString());
            }
        }

        [HttpGet("template")]
        public async Task<IActionResult> GetTemplate()
        {
            try
            {
                var templateBytes = _batchProcessingService.GenerateTemplateFile();
                return File(templateBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Questions_Template.xlsx");
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, EnumLoggingType.Error.ToString());
            }
        }

        [HttpGet("test-questions")]
        public async Task<IActionResult> GetTestQuestions()
        {
            try
            {
                var testQuestionsBytes = _batchProcessingService.Generate50TestQuestionsFile();
                return File(testQuestionsBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "50_Test_Questions.xlsx");
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, EnumLoggingType.Error.ToString());
            }
        }
    }
}