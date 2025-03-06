using DynamicDashboardCommon.Models;
using DynamicDasboardWebAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DynamicDashboardCommon.Models.DynamicDashboardCommon.Models;

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
    public class EnhancedNlQueryController : ControllerBase
    {
        private readonly EnhancedNlQueryService _nlQueryService;
        private readonly ILogger<EnhancedNlQueryController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="EnhancedNlQueryController"/> class.
        /// </summary>
        /// <param name="nlQueryService">The natural language query service.</param>
        /// <param name="logger">The logger.</param>
        public EnhancedNlQueryController(
            EnhancedNlQueryService nlQueryService,
            ILogger<EnhancedNlQueryController> logger)
        {
            _nlQueryService = nlQueryService ?? throw new ArgumentNullException(nameof(nlQueryService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Step 1: Analyzes a natural language question and returns an explanation.
        /// </summary>
        /// <param name="request">The natural language query request.</param>
        /// <returns>An explanation of how the system understands the question.</returns>
        [HttpPost("analyze")]
        public async Task<ActionResult<AnalysisResponse>> AnalyzeQuestion([FromBody] NlQueryRequest request)
        {
            if (request == null)
            {
                return BadRequest("Request cannot be null.");
            }

            if (string.IsNullOrWhiteSpace(request.Question))
            {
                return BadRequest("Question cannot be empty.");
            }

            try
            {
                _logger.LogInformation("Analyzing question: {Question}", request.Question);
                var response = await _nlQueryService.AnalyzeQuestionAsync(request);

                if (response.Success)
                {
                    return Ok(response);
                }
                else
                {
                    _logger.LogWarning("Analysis failed: {ErrorMessage}", response.ErrorMessage);
                    return StatusCode(500, response);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing question: {Question}", request.Question);
                return StatusCode(500, new AnalysisResponse
                {
                    Question = request.Question,
                    Success = false,
                    ErrorMessage = $"An unexpected error occurred: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Step 2: Generates SQL from a confirmed understanding.
        /// </summary>
        /// <param name="request">The confirmation request with resolved ambiguities.</param>
        /// <returns>The generated SQL query.</returns>
        [HttpPost("generate")]
        public async Task<ActionResult<SqlGenerationResponse>> GenerateSql([FromBody] NlQueryConfirmationRequest request)
        {
            if (request == null)
            {
                return BadRequest("Request cannot be null.");
            }

            if (string.IsNullOrWhiteSpace(request.OriginalQuestion))
            {
                return BadRequest("Original question cannot be empty.");
            }

            if (string.IsNullOrWhiteSpace(request.ConfirmedUnderstanding))
            {
                return BadRequest("Confirmed understanding cannot be empty.");
            }

            try
            {
                _logger.LogInformation("Generating SQL for question: {Question}", request.OriginalQuestion);
                var response = await _nlQueryService.GenerateSqlAsync(request);

                if (response.Success)
                {
                    return Ok(response);
                }
                else
                {
                    _logger.LogWarning("SQL generation failed: {ErrorMessage}", response.ErrorMessage);
                    return StatusCode(500, response);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating SQL for question: {Question}", request.OriginalQuestion);
                return StatusCode(500, new SqlGenerationResponse
                {
                    OriginalQuestion = request.OriginalQuestion,
                    Success = false,
                    ErrorMessage = $"An unexpected error occurred: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Step 3: Executes a SQL query and returns the results.
        /// </summary>
        /// <param name="request">The execution request with the SQL query.</param>
        /// <returns>The query results with explanation.</returns>
        [HttpPost("execute")]
        public async Task<ActionResult<QueryExecutionResponse>> ExecuteQuery([FromBody] SqlExecutionRequest request)
        {
            if (request == null)
            {
                return BadRequest("Request cannot be null.");
            }

            if (string.IsNullOrWhiteSpace(request.Sql))
            {
                return BadRequest("SQL query cannot be empty.");
            }

            try
            {
                _logger.LogInformation("Executing SQL query for database ID: {DatabaseId}", request.DatabaseId);
                var response = await _nlQueryService.ExecuteQueryAsync(request);

                if (response.Success)
                {
                    return Ok(response);
                }
                else
                {
                    _logger.LogWarning("Query execution failed: {ErrorMessage}", response.ErrorMessage);
                    return StatusCode(500, response);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing SQL for database ID: {DatabaseId}", request.DatabaseId);
                return StatusCode(500, new QueryExecutionResponse
                {
                    OriginalQuestion = request.OriginalQuestion,
                    Sql = request.Sql,
                    Success = false,
                    ErrorMessage = $"An unexpected error occurred: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Unified endpoint for backward compatibility: processes a natural language query in one step.
        /// </summary>
        /// <param name="request">The natural language query request.</param>
        /// <returns>The query response including SQL and results.</returns>
        [HttpPost("process")]
        public async Task<ActionResult<NlQueryResponse>> ProcessQuery([FromBody] NlQueryRequest request)
        {
            if (request == null)
            {
                return BadRequest("Request cannot be null.");
            }

            if (string.IsNullOrWhiteSpace(request.Question))
            {
                return BadRequest("Question cannot be empty.");
            }

            try
            {
                _logger.LogInformation("Processing query in one step: {Question}", request.Question);
                var response = await _nlQueryService.ProcessNaturalLanguageQueryAsync(request);

                if (response.Success)
                {
                    return Ok(response);
                }
                else
                {
                    _logger.LogWarning("Query processing failed: {ErrorMessage}", response.ErrorMessage);
                    return StatusCode(500, response);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing query: {Question}", request.Question);
                return StatusCode(500, new NlQueryResponse
                {
                    FormattedQuestion = request.Question,
                    Success = false,
                    ErrorMessage = $"An unexpected error occurred: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Gets example questions for a specific database.
        /// </summary>
        /// <param name="databaseId">The database ID.</param>
        /// <returns>A list of example questions.</returns>
        [HttpGet("examples/{databaseId}")]
        public ActionResult<List<string>> GetExampleQuestions(int databaseId)
        {
            try
            {
                // Generate examples based on database schema (simplified for now)
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
                _logger.LogError(ex, "Error getting example questions for database ID: {DatabaseId}", databaseId);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }
    }
}