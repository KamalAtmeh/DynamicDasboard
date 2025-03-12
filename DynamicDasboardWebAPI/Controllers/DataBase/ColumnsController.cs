using DynamicDashboardCommon.Models;
using DynamicDasboardWebAPI.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using DynamicDashboardCommon.Enums;

namespace DynamicDasboardWebAPI.Controllers
{
    /// <summary>
    /// API Controller for managing columns in the dynamic dashboard.
    /// Provides endpoints to get, add, update, and delete columns.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ColumnsController : AppControllerBase
    {
        private readonly ColumnService _service;

        /// <summary>
        /// Initializes a new instance of the <see cref="ColumnsController"/> class.
        /// </summary>
        /// <param name="service">The column service to handle business logic.</param>
        /// <param name="logsService">Service for logging exceptions.</param>
        public ColumnsController(ColumnService service, ILogsService logsService)
            : base(logsService)
        {
            _service = service;
        }

        /// <summary>
        /// Gets the columns for a specific table.
        /// </summary>
        /// <param name="tableId">The ID of the table.</param>
        /// <returns>A list of columns for the specified table.</returns>
        [HttpGet("table/{tableId}")]
        public async Task<IActionResult> GetColumnsByTableId(int tableId)
        {
            try
            {
                var columns = await _service.GetColumnsByTableIdAsync(tableId);
                return Ok(columns);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, EnumLoggingType.Error.ToString());
            }
        }

        /// <summary>
        /// Adds a new column.
        /// </summary>
        /// <param name="column">The column to add.</param>
        /// <returns>The ID of the newly added column.</returns>
        [HttpPost]
        public async Task<IActionResult> AddColumn([FromBody] Column column)
        {
            try
            {
                var result = await _service.AddColumnAsync(column);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, EnumLoggingType.Error.ToString());
            }
        }

        /// <summary>
        /// Updates an existing column.
        /// </summary>
        /// <param name="columnId">The ID of the column to update.</param>
        /// <param name="column">The updated column data.</param>
        /// <returns>The ID of the updated column.</returns>
        [HttpPut("{columnId}")]
        public async Task<IActionResult> UpdateColumn(int columnId, [FromBody] Column column)
        {
            try
            {
                if (columnId != column.ColumnID)
                    return BadRequest("Column ID mismatch.");

                var result = await _service.UpdateColumnAsync(column);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, EnumLoggingType.Error.ToString());
            }
        }

        /// <summary>
        /// Deletes a column.
        /// </summary>
        /// <param name="columnId">The ID of the column to delete.</param>
        /// <returns>The ID of the deleted column.</returns>
        [HttpDelete("{columnId}")]
        public async Task<IActionResult> DeleteColumn(int columnId)
        {
            try
            {
                var result = await _service.DeleteColumnAsync(columnId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, EnumLoggingType.Error.ToString());
            }
        }
    }
}
