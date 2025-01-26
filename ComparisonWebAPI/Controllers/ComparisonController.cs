using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using DynamicDasboardWebAPI.Services;
using DynamicDashboardCommon.Models;
using Microsoft.Extensions.Logging;
using System.Data.SqlClient;

namespace DynamicDasboardWebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ComparisonController : ControllerBase
    {
        private readonly ComparisonService _queryComparisonService;
        private readonly ILogger<ComparisonController> _logger;

        public ComparisonController(ComparisonService queryComparisonService, ILogger<ComparisonController> logger)
        {
            _queryComparisonService = queryComparisonService;
            _logger = logger;
        }

        [HttpPost("compare")]
        public async Task<IActionResult> CompareQueries([FromBody] QueryComparison request)
        {
            if (request == null || string.IsNullOrEmpty(request.GptSqlQuery) || string.IsNullOrEmpty(request.DeepSeekSqlQuery) || string.IsNullOrEmpty(request.DeepSeekChatSqlQuery))
            {
                return BadRequest("Invalid request. Queries cannot be null or empty.");
            }

            try
            {
                // Execute the queries asynchronously
                request.GptDataResult = await _queryComparisonService.ExecuteQueryAsync(request.GptSqlQuery);
                request.DeepSeekDataResult = await _queryComparisonService.ExecuteQueryAsync(request.DeepSeekSqlQuery);
                request.DeepSeekChatDataResult = await _queryComparisonService.ExecuteQueryAsync(request.DeepSeekChatSqlQuery);

                // Compare the datasets
                request.GptVsDeepSeekChatComparison = _queryComparisonService.CompareDatasets(request.GptDataResult, request.DeepSeekChatDataResult);
                request.DeepSeekVsDeepSeekChatComparison = _queryComparisonService.CompareDatasets(request.DeepSeekDataResult, request.DeepSeekChatDataResult);

                // Return the comparison results
                return Ok(request);
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Database error occurred while executing queries.");
                return StatusCode(500, "A database error occurred. Please try again later.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred.");
                return StatusCode(500, "An unexpected error occurred. Please try again later.");
            }
        }
    }
}