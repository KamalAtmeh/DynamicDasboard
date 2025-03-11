using DynamicDashboardCommon.Models;
using DynamicDasboardWebAPI.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using DynamicDashboardCommon.Enums;

namespace DynamicDasboardWebAPI.Controllers
{
    /// <summary>
    /// API Controller for managing tables in the database.
    /// Provides endpoints to get, add, update, and delete tables.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class TablesController : AppControllerBase
    {
        private readonly TableService _service;

        /// <summary>
        /// Initializes a new instance of the <see cref="TablesController"/> class.
        /// </summary>
        /// <param name="service">The table service to interact with the data layer.</param>
        /// <param name="logsService">The logs service for handling exceptions.</param>
        public TablesController(TableService service, ILogsService logsService)
            : base(logsService)
        {
            _service = service;
        }

        /// <summary>
        /// Gets the list of tables for a specific database.
        /// </summary>
        /// <param name="databaseId">The ID of the database.</param>
        [HttpGet("database/{databaseId}")]
        public async Task<IActionResult> GetTablesByDatabaseId(int databaseId)
        {
            try
            {
                var tables = await _service.GetTablesByDatabaseIdAsync(databaseId);
                return Ok(tables);
            }
            catch (System.Exception ex)
            {
                return await HandleExceptionAsync(ex, LoggingType.Error.ToString());
            }
        }

        /// <summary>
        /// Adds a new table to the database.
        /// </summary>
        /// <param name="table">The table to add.</param>
        [HttpPost]
        public async Task<IActionResult> AddTable([FromBody] Table table)
        {
            try
            {
                var result = await _service.AddTableAsync(table);
                return Ok(result);
            }
            catch (System.Exception ex)
            {
                return await HandleExceptionAsync(ex, LoggingType.Error.ToString());
            }
        }

        /// <summary>
        /// Updates an existing table in the database.
        /// </summary>
        /// <param name="tableId">The ID of the table to update.</param>
        /// <param name="table">The updated table data.</param>
        [HttpPut("{tableId}")]
        public async Task<IActionResult> UpdateTable(int tableId, [FromBody] Table table)
        {
            try
            {
                if (tableId != table.TableID)
                    return BadRequest("Table ID mismatch.");

                var result = await _service.UpdateTableAsync(table);
                return Ok(result);
            }
            catch (System.Exception ex)
            {
                return await HandleExceptionAsync(ex, LoggingType.Error.ToString());
            }
        }

        /// <summary>
        /// Deletes a table from the database.
        /// </summary>
        /// <param name="tableId">The ID of the table to delete.</param>
        [HttpDelete("{tableId}")]
        public async Task<IActionResult> DeleteTable(int tableId)
        {
            try
            {
                var result = await _service.DeleteTableAsync(tableId);
                return Ok(result);
            }
            catch (System.Exception ex)
            {
                return await HandleExceptionAsync(ex, LoggingType.Error.ToString());
            }
        }
    }
}
