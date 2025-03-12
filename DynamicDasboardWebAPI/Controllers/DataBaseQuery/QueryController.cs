using DynamicDashboardCommon.Models;
using DynamicDasboardWebAPI.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DynamicDashboardCommon.Models.DynamicDashboardCommon.Models;
using DynamicDashboardCommon.Enums;

namespace DynamicDasboardWebAPI.Controllers
{
    /// <summary>
    /// Enhanced controller for processing natural language queries with a multi-step workflow:
    /// 1. Analyze question and provide explanation
    /// 2. Generate SQL from confirmed understanding
    /// 3. Execute query and return results
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class QueryController : AppControllerBase
    {
        private readonly QueryService _nlQueryService;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryController"/> class.
        /// </summary>
        /// <param name="nlQueryService">The natural language query service.</param>
        /// <param name="logsService">Service for logs and exception handling.</param>
        public QueryController(QueryService nlQueryService, ILogsService logsService)
            : base(logsService)
        {
            _nlQueryService = nlQueryService ?? throw new ArgumentNullException(nameof(nlQueryService));
        }

        /// <summary>
        /// Step 1: Analyzes a natural language question and returns an explanation.
        /// </summary>
        [HttpPost("analyze")]
        public async Task<IActionResult> AnalyzeQuestion([FromBody] NlQueryRequest request)
        {
            if (request == null)
                return BadRequest("Request cannot be null.");

            if (string.IsNullOrWhiteSpace(request.Question))
                return BadRequest("Question cannot be empty.");

            try
            {
                var response = await _nlQueryService.AnalyzeQuestionAsync(request);

                if (response.Success)
                {
                    return Ok(response);
                }
                else
                {
                    // Return 500 with the response, but no exception thrown here
                    return StatusCode(500, response);
                }
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, EnumLoggingType.Error.ToString());
            }
        }

        /// <summary>
        /// Step 2: Generates SQL from a confirmed understanding.
        /// </summary>
        [HttpPost("generate")]
        public async Task<IActionResult> GenerateSql([FromBody] NlQueryConfirmationRequest request)
        {
            if (request == null)
                return BadRequest("Request cannot be null.");

            if (string.IsNullOrWhiteSpace(request.OriginalQuestion))
                return BadRequest("Original question cannot be empty.");

            if (string.IsNullOrWhiteSpace(request.ConfirmedUnderstanding))
                return BadRequest("Confirmed understanding cannot be empty.");

            try
            {
                var response = await _nlQueryService.GenerateSqlAsync(request);

                if (response.Success)
                {
                    return Ok(response);
                }
                else
                {
                    return StatusCode(500, response);
                }
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, EnumLoggingType.Error.ToString());
            }
        }

        /// <summary>
        /// Step 3: Executes a SQL query and returns the results.
        /// </summary>
        [HttpPost("execute")]
        public async Task<IActionResult> ExecuteQuery([FromBody] SqlExecutionRequest request)
        {
            if (request == null)
                return BadRequest("Request cannot be null.");

            if (string.IsNullOrWhiteSpace(request.Sql))
                return BadRequest("SQL query cannot be empty.");

            try
            {
                var response = await _nlQueryService.ExecuteQueryAsync(request);

                if (response.Success)
                {
                    return Ok(response);
                }
                else
                {
                    return StatusCode(500, response);
                }
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, EnumLoggingType.Error.ToString());
            }
        }

        /// <summary>
        /// Unified endpoint for backward compatibility: processes a natural language query in one step.
        /// </summary>
        [HttpPost("process")]
        public async Task<IActionResult> ProcessQuery([FromBody] NlQueryRequest request)
        {


            try
            {
                var response = await _nlQueryService.ProcessNaturalLanguageQueryAsync(request);

                if (response.Success)
                {
                    return Ok(response);
                }
                else
                {
                    throw new Exception("Error processing query");
                }
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, EnumLoggingType.Error.ToString());
            }
        }

        /// <summary>
        /// Gets example questions for a specific database.
        /// </summary>
        [HttpGet("examples/{databaseId}")]
        public async Task<IActionResult> GetExampleQuestions(int databaseId)
        {
            try
            {
                // Basic example questions. This is a synchronous call.
                var examples = new List<string>
                {
                    "Show me the top 10 customers by total order value",
                    "What is the average order value by product category?",
                    "How many orders were placed last month?",
                    "List all products with less than 10 items in stock",
                    "Which employees had the highest sales in the last quarter?",
                    "Show me customers who haven't made a purchase in the last 6 months",
                    "What is our revenue trend by month for this year?",
                    "Compare sales performance across different regions",
                    "Find products that are frequently purchased together",
                    "Which marketing campaigns had the highest ROI?"
                };

                return Ok(examples);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, EnumLoggingType.Error.ToString());
            }
        }
    }
}
