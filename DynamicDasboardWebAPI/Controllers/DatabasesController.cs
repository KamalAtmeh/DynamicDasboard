using DynamicDasboardWebAPI.Services;
using DynamicDashboardCommon.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DynamicDasboardWebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DatabasesController : ControllerBase
    {
        private readonly DatabaseService _service;

        public DatabasesController(DatabaseService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Database>>> GetAllDatabases()
        {
            var databases = await _service.GetAllDatabasesAsync();
            return Ok(databases);
        }

        [HttpPost]
        public async Task<ActionResult<int>> AddDatabase([FromBody] Database database)
        {
            var result = await _service.AddDatabaseAsync(database);
            return Ok(result);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<int>> UpdateDatabase(int id, [FromBody] Database database)
        {
            if (id != database.DatabaseID)
                return BadRequest("Database ID mismatch.");

            var result = await _service.UpdateDatabaseAsync(database);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<int>> DeleteDatabase(int id)
        {
            var result = await _service.DeleteDatabaseAsync(id);
            return Ok(result);
        }

        [HttpPost("test-connection")]
        public async Task<ActionResult<bool>> TestConnection([FromBody] Database database)
        {
            var result = await _service.TestConnectionAsync(database);
            return Ok(result);
        }
    }
}