using DynamicDashboardCommon.Models;
using DynamicDasboardWebAPI.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DynamicDasboardWebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class QueryController : ControllerBase
    {
        private readonly QueryService _queryService;
        private readonly ILogger<QueryController> _logger;

        public QueryController(QueryService queryService, ILogger<QueryController> logger)
        {
            _queryService = queryService;
            _logger = logger;
        }

        [HttpPost("execute")]
        public async Task<ActionResult<DirectSqlResult>> ExecuteQuery([FromBody] DirectSqlRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.SqlQuery))
            {
                return BadRequest(new DirectSqlResult
                {
                    Success = false,
                    ErrorMessage = "SQL query cannot be null or empty"
                });
            }

            try
            {
                var result = await _queryService.ExecuteDirectQueryAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing SQL query");
                return StatusCode(500, new DirectSqlResult
                {
                    Success = false,
                    ErrorMessage = $"Error executing query: {ex.Message}"
                });
            }
        }
    }
}