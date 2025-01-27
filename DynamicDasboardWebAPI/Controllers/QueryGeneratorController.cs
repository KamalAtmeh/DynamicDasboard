using DynamicDasboardWebAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Threading.Tasks;
using DynamicDashboardCommon.Models;

namespace DynamicDasboardWebAPI.Controllers
{
    /// <summary>
    /// API Controller for handling query generation and Excel file processing.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class QueryGeneratorController : ControllerBase
    {
        private readonly QueryGeneratorService _queryGeneratorService;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryGeneratorController"/> class.
        /// </summary>
        /// <param name="queryGeneratorService">The service for generating queries and processing Excel files.</param>
        public QueryGeneratorController(QueryGeneratorService queryGeneratorService)
        {
            _queryGeneratorService = queryGeneratorService;
        }

        /// <summary>
        /// Generates a query based on the provided schema and question.
        /// </summary>
        /// <param name="request">The request containing the schema and question.</param>
        /// <returns>An IActionResult containing the generated query or an error message.</returns>
        [HttpPost("generate")]
        public async Task<IActionResult> GenerateQuery([FromBody] QueryRequest request)
        {
            // Validate the request parameters
            if (string.IsNullOrEmpty(request.Schema) || string.IsNullOrEmpty(request.Question))
            {
                return BadRequest("Schema and question are required.");
            }

            // Generate the query using the service
            var result = await _queryGeneratorService.GenerateQuery(request.Schema, request.Question);
            return result;
        }

        /// <summary>
        /// Processes an uploaded Excel file and returns a modified version.
        /// </summary>
        /// <param name="file">The uploaded Excel file.</param>
        /// <returns>An IActionResult containing the modified Excel file or an error message.</returns>
        [HttpPost("process-excel")]
        public async Task<IActionResult> ProcessExcel([FromForm] IFormFile file)
        {
            // Validate the uploaded file
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            // Process the Excel file using the service
            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                var modifiedFile = await _queryGeneratorService.ProcessExcelFile(stream);
                return File(modifiedFile, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Modified_Queries.xlsx");
            }
        }
    }
}