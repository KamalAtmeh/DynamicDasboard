using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using DynamicDasboardWebAPI.Services;
using DynamicDashboardCommon.Models;
using Microsoft.Extensions.Logging;
using System.Data.SqlClient;

namespace DynamicDasboardWebAPI.Controllers
{
    /// <summary>
    /// Controller for handling query comparison requests.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ComparisonController : ControllerBase
    {
        private readonly ComparisonService _queryComparisonService;
        private readonly ILogger<ComparisonController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ComparisonController"/> class.
        /// </summary>
        /// <param name="queryComparisonService">The service for executing and comparing queries.</param>
        /// <param name="logger">The logger instance used for logging information, warnings, and errors.</param>
        public ComparisonController(ComparisonService queryComparisonService, ILogger<ComparisonController> logger)
        {
            _queryComparisonService = queryComparisonService;
            _logger = logger;
        }

        /// <summary>
        /// Compares the results of three SQL queries provided in the request.
        /// </summary>
        /// <param name="request">The request containing the SQL queries to compare and other related information.</param>
        /// <returns>An <see cref="IActionResult"/> containing the comparison results or an error message.</returns>
        [HttpPost("compare")]
        public async Task<IActionResult> CompareQueries([FromBody] QueryComparison request)
        {
            // Validate the request to ensure it is not null and contains all required SQL queries
            if (request == null || string.IsNullOrEmpty(request.GptSqlQuery) || string.IsNullOrEmpty(request.DeepSeekSqlQuery) || string.IsNullOrEmpty(request.DeepSeekChatSqlQuery))
            {
                return BadRequest("Invalid request. Queries cannot be null or empty.");
            }

            try
            {
                // Execute the GPT SQL query asynchronously and store the result
                request.GptDataResult = await _queryComparisonService.ExecuteQueryAsync(request.GptSqlQuery);

                // Execute the DeepSeek SQL query asynchronously and store the result
                request.DeepSeekDataResult = await _queryComparisonService.ExecuteQueryAsync(request.DeepSeekSqlQuery);

                // Execute the DeepSeek Chat SQL query asynchronously and store the result
                request.DeepSeekChatDataResult = await _queryComparisonService.ExecuteQueryAsync(request.DeepSeekChatSqlQuery);

                // Compare the results of the GPT query and the DeepSeek Chat query
                request.GptVsDeepSeekChatComparison = _queryComparisonService.CompareDatasets(request.GptDataResult, request.DeepSeekChatDataResult);

                // Compare the results of the DeepSeek query and the DeepSeek Chat query
                request.DeepSeekVsDeepSeekChatComparison = _queryComparisonService.CompareDatasets(request.DeepSeekDataResult, request.DeepSeekChatDataResult);

                // Return the request object containing the comparison results
                return Ok(request);
            }
            catch (SqlException ex)
            {
                // Log the SQL exception and return a 500 status code with a database error message
                _logger.LogError(ex, "Database error occurred while executing queries.");
                return StatusCode(500, "A database error occurred. Please try again later.");
            }
            catch (Exception ex)
            {
                // Log any other exceptions and return a 500 status code with a generic error message
                _logger.LogError(ex, "An unexpected error occurred.");
                return StatusCode(500, "An unexpected error occurred. Please try again later.");
            }
        }
    }
}