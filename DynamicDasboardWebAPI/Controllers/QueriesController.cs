using DynamicDasboardWebAPI.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;


namespace DynamicDasboardWebAPI.Controllers
{
    /// <summary>
    /// API Controller for handling query execution requests.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class QueryController : ControllerBase
    {
        private readonly QueryService _service;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryController"/> class.
        /// </summary>
        /// <param name="service">The query service to be used for executing queries.</param>
        public QueryController(QueryService service)
        {
            _service = service;
        }

        /// <summary>
        /// Executes a SQL query based on the provided query string and database type.
        /// </summary>
        /// <param name="query">The SQL query to be executed.</param>
        /// <param name="databaseType">The type of database to execute the query against.</param>
        /// <param name="executedBy">Optional user ID of the executor.</param>
        /// <returns>The result of the executed query.</returns>
        [HttpPost("execute")]
        public async Task<IActionResult> ExecuteQuery([FromBody] string query, [FromQuery] string databaseType, [FromQuery] int? executedBy = null)
        {
            try
            {
                var result = await _service.ExecuteQueryAsync(query, databaseType, executedBy);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
