using DynamicDashboardCommon.Models;
using DynamicDasboardWebAPI.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DynamicDasboardWebAPI.Controllers
{
    /// <summary>
    /// API Controller for managing tables in the database.
    /// Provides endpoints to get, add, update, and delete tables.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class TablesController : ControllerBase
    {
        private readonly TableService _service;

        /// <summary>
        /// Initializes a new instance of the <see cref="TablesController"/> class.
        /// </summary>
        /// <param name="service">The table service to interact with the data layer.</param>
        public TablesController(TableService service)
        {
            _service = service;
        }

        /// <summary>
        /// Gets the list of tables for a specific database.
        /// </summary>
        /// <param name="databaseId">The ID of the database.</param>
        /// <returns>A list of tables in the specified database.</returns>
        [HttpGet("database/{databaseId}")]
        public async Task<ActionResult<IEnumerable<Table>>> GetTablesByDatabaseId(int databaseId)
        {
            var tables = await _service.GetTablesByDatabaseIdAsync(databaseId);
            return Ok(tables);
        }

        /// <summary>
        /// Adds a new table to the database.
        /// </summary>
        /// <param name="table">The table to add.</param>
        /// <returns>The ID of the newly created table.</returns>
        [HttpPost]
        public async Task<ActionResult<int>> AddTable([FromBody] Table table)
        {
            var result = await _service.AddTableAsync(table);
            return Ok(result);
        }

        /// <summary>
        /// Updates an existing table in the database.
        /// </summary>
        /// <param name="tableId">The ID of the table to update.</param>
        /// <param name="table">The updated table data.</param>
        /// <returns>The ID of the updated table.</returns>
        [HttpPut("{tableId}")]
        public async Task<ActionResult<int>> UpdateTable(int tableId, [FromBody] Table table)
        {
            if (tableId != table.TableID)
                return BadRequest("Table ID mismatch.");

            var result = await _service.UpdateTableAsync(table);
            return Ok(result);
        }

        /// <summary>
        /// Deletes a table from the database.
        /// </summary>
        /// <param name="tableId">The ID of the table to delete.</param>
        /// <returns>The ID of the deleted table.</returns>
        [HttpDelete("{tableId}")]
        public async Task<ActionResult<int>> DeleteTable(int tableId)
        {
            var result = await _service.DeleteTableAsync(tableId);
            return Ok(result);
        }
    }
}