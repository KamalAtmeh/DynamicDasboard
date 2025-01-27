using DynamicDashboardCommon.Models;
using DynamicDasboardWebAPI.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DynamicDasboardWebAPI.Controllers
{
    /// <summary>
    /// API Controller for managing columns in the dynamic dashboard.
    /// Provides endpoints to get, add, update, and delete columns.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ColumnsController : ControllerBase
    {
        private readonly ColumnService _service;

        /// <summary>
        /// Initializes a new instance of the <see cref="ColumnsController"/> class.
        /// </summary>
        /// <param name="service">The column service to handle business logic.</param>
        public ColumnsController(ColumnService service)
        {
            _service = service;
        }

        /// <summary>
        /// Gets the columns for a specific table.
        /// </summary>
        /// <param name="tableId">The ID of the table.</param>
        /// <returns>A list of columns for the specified table.</returns>
        [HttpGet("table/{tableId}")]
        public async Task<ActionResult<IEnumerable<Column>>> GetColumnsByTableId(int tableId)
        {
            var columns = await _service.GetColumnsByTableIdAsync(tableId);
            return Ok(columns);
        }

        /// <summary>
        /// Adds a new column.
        /// </summary>
        /// <param name="column">The column to add.</param>
        /// <returns>The ID of the newly added column.</returns>
        [HttpPost]
        public async Task<ActionResult<int>> AddColumn([FromBody] Column column)
        {
            var result = await _service.AddColumnAsync(column);
            return Ok(result);
        }

        /// <summary>
        /// Updates an existing column.
        /// </summary>
        /// <param name="columnId">The ID of the column to update.</param>
        /// <param name="column">The updated column data.</param>
        /// <returns>The ID of the updated column.</returns>
        [HttpPut("{columnId}")]
        public async Task<ActionResult<int>> UpdateColumn(int columnId, [FromBody] Column column)
        {
            if (columnId != column.ColumnID)
                return BadRequest("Column ID mismatch.");

            var result = await _service.UpdateColumnAsync(column);
            return Ok(result);
        }

        /// <summary>
        /// Deletes a column.
        /// </summary>
        /// <param name="columnId">The ID of the column to delete.</param>
        /// <returns>The ID of the deleted column.</returns>
        [HttpDelete("{columnId}")]
        public async Task<ActionResult<int>> DeleteColumn(int columnId)
        {
            var result = await _service.DeleteColumnAsync(columnId);
            return Ok(result);
        }
    }
}