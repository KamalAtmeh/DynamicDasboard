using DynamicDashboardCommon.Models;
using DynamicDasboardWebAPI.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DynamicDasboardWebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TablesController : ControllerBase
    {
        private readonly TableService _service;

        public TablesController(TableService service)
        {
            _service = service;
        }

        // Get tables for a specific database
        [HttpGet("database/{databaseId}")]
        public async Task<ActionResult<IEnumerable<Table>>> GetTablesByDatabaseId(int databaseId)
        {
            var tables = await _service.GetTablesByDatabaseIdAsync(databaseId);
            return Ok(tables);
        }

        // Add a new table
        [HttpPost]
        public async Task<ActionResult<int>> AddTable([FromBody] Table table)
        {
            var result = await _service.AddTableAsync(table);
            return Ok(result);
        }

        // Update an existing table
        [HttpPut("{tableId}")]
        public async Task<ActionResult<int>> UpdateTable(int tableId, [FromBody] Table table)
        {
            if (tableId != table.TableID)
                return BadRequest("Table ID mismatch.");

            var result = await _service.UpdateTableAsync(table);
            return Ok(result);
        }

        // Delete a table
        [HttpDelete("{tableId}")]
        public async Task<ActionResult<int>> DeleteTable(int tableId)
        {
            var result = await _service.DeleteTableAsync(tableId);
            return Ok(result);
        }
    }
}