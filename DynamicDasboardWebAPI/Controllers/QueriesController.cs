using DynamicDasboardWebAPI.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;


namespace DynamicDasboardWebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class QueryController : ControllerBase
    {
        private readonly QueryService _service;

        public QueryController(QueryService service)
        {
            _service = service;
        }

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

        //[HttpPost("execute")]
        //public async Task<IActionResult> ExecuteQuery([FromBody] string query, [FromQuery] string databaseType, [FromQuery] int? executedBy = null)
        //{
        //    try
        //    {
        //        var result = await _service.ExecuteQueryAsync(query, databaseType, executedBy);
        //        return Ok(result);
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(ex.Message);
        //    }
        //}
    }
}
