using DynamicDasboardWebAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Threading.Tasks;
using DynamicDashboardCommon.Models;

namespace DynamicDasboardWebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class QueryGeneratorController : ControllerBase
    {
        private readonly QueryGeneratorService _queryGeneratorService;

        public QueryGeneratorController(QueryGeneratorService queryGeneratorService)
        {
            _queryGeneratorService = queryGeneratorService;
        }

        [HttpPost("generate")]
        public async Task<IActionResult> GenerateQuery([FromBody] QueryRequest request)
        {
            if (string.IsNullOrEmpty(request.Schema) || string.IsNullOrEmpty(request.Question))
            {
                return BadRequest("Schema and question are required.");
            }

            var result = await _queryGeneratorService.GenerateQuery(request.Schema, request.Question);
            return result;
        }

        [HttpPost("process-excel")]
        public async Task<IActionResult> ProcessExcel([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                var modifiedFile = await _queryGeneratorService.ProcessExcelFile(stream);
                return File(modifiedFile, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Modified_Queries.xlsx");
            }
        }
    }
}