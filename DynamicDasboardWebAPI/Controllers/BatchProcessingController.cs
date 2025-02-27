using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Threading.Tasks;
using DynamicDasboardWebAPI.Services;
using Microsoft.Extensions.Logging;

namespace DynamicDasboardWebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BatchProcessingController : ControllerBase
    {
        private readonly BatchProcessingService _batchProcessingService;
        private readonly ILogger<BatchProcessingController> _logger;

        public BatchProcessingController(
            BatchProcessingService batchProcessingService,
            ILogger<BatchProcessingController> logger)
        {
            _batchProcessingService = batchProcessingService;
            _logger = logger;
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
                _logger.LogError(ex, "Error processing Excel file");
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        [HttpGet("template")]
        public IActionResult GetTemplate()
        {
            try
            {
                var templateBytes = _batchProcessingService.GenerateTemplateFile();
                return File(templateBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Questions_Template.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating template file");
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        [HttpGet("test-questions")]
        public IActionResult GetTestQuestions()
        {
            try
            {
                var testQuestionsBytes = _batchProcessingService.Generate50TestQuestionsFile();
                return File(testQuestionsBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "50_Test_Questions.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating test questions file");
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }
    }
}