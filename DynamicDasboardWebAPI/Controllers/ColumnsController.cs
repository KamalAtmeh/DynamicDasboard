using DynamicDashboardCommon.Models;
using DynamicDasboardWebAPI.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DynamicDasboardWebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ColumnsController : ControllerBase
    {
        private readonly ColumnService _service;

        public ColumnsController(ColumnService service)
        {
            _service = service;
        }

        // Get columns for a specific table
        [HttpGet("table/{tableId}")]
        public async Task<ActionResult<IEnumerable<Column>>> GetColumnsByTableId(int tableId)
        {
            var columns = await _service.GetColumnsByTableIdAsync(tableId);
            return Ok(columns);
        }

        // Add a new column
        [HttpPost]
        public async Task<ActionResult<int>> AddColumn([FromBody] Column column)
        {
            var result = await _service.AddColumnAsync(column);
            return Ok(result);
        }

        // Update an existing column
        [HttpPut("{columnId}")]
        public async Task<ActionResult<int>> UpdateColumn(int columnId, [FromBody] Column column)
        {
            if (columnId != column.ColumnID)
                return BadRequest("Column ID mismatch.");

            var result = await _service.UpdateColumnAsync(column);
            return Ok(result);
        }

        // Delete a column
        [HttpDelete("{columnId}")]
        public async Task<ActionResult<int>> DeleteColumn(int columnId)
        {
            var result = await _service.DeleteColumnAsync(columnId);
            return Ok(result);
        }
    }
}