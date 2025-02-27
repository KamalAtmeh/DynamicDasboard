using DynamicDashboardCommon.Models;
using DynamicDasboardWebAPI.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DynamicDasboardWebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NlQueryController : ControllerBase
    {
        private readonly NlQueryService _nlQueryService;
        private readonly ILogger<NlQueryController> _logger;

        public NlQueryController(NlQueryService nlQueryService, ILogger<NlQueryController> logger)
        {
            _nlQueryService = nlQueryService;
            _logger = logger;
        }

        [HttpPost("process")]
        public async Task<ActionResult<NlQueryResponse>> ProcessQuery([FromBody] NlQueryRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Question) || request.DatabaseId <= 0)
            {
                return BadRequest("Invalid request. Question and DatabaseId are required.");
            }

            try
            {
                var response = await _nlQueryService.ProcessNaturalLanguageQueryAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing natural language query.");
                return StatusCode(500, new NlQueryResponse
                {
                    Success = false,
                    ErrorMessage = "An unexpected error occurred. Please try again later."
                });
            }
        }

        [HttpGet("examples/{databaseId}")]
        public ActionResult<List<string>> GetExampleQuestions(int databaseId)
        {
            try
            {
                // In a real implementation, this would generate examples based on the database schema
                // For now, we'll return some sample questions
                var examples = new List<string>
                {
                    "Show me the top 10 customers by total order value",
                    "What is the average order value by product category?",
                    "How many orders were placed last month?",
                    "List all products with less than 10 items in stock",
                    "Which employees had the highest sales in the last quarter?"
                };

                return Ok(examples);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting example questions.");
                return StatusCode(500, "An unexpected error occurred.");
            }
        }
    }
}