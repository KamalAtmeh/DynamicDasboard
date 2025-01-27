using DynamicDasboardWebAPI.Services;
using DynamicDashboardCommon.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DynamicDasboardWebAPI.Controllers
{
    /// <summary>
    /// API Controller for managing database connections.
    /// Provides endpoints to perform CRUD operations and test database connections.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class DatabasesController : ControllerBase
    {
        private readonly DatabaseService _service;

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabasesController"/> class.
        /// </summary>
        /// <param name="service">The database service to interact with the database repository.</param>
        public DatabasesController(DatabaseService service)
        {
            _service = service;
        }

        /// <summary>
        /// Gets all databases.
        /// </summary>
        /// <returns>A list of all databases.</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Database>>> GetAllDatabases()
        {
            var databases = await _service.GetAllDatabasesAsync();
            return Ok(databases);
        }

        /// <summary>
        /// Adds a new database.
        /// </summary>
        /// <param name="database">The database entity to add.</param>
        /// <returns>The ID of the newly added database.</returns>
        [HttpPost]
        public async Task<ActionResult<int>> AddDatabase([FromBody] Database database)
        {
            var result = await _service.AddDatabaseAsync(database);
            return Ok(result);
        }

        /// <summary>
        /// Updates an existing database.
        /// </summary>
        /// <param name="id">The ID of the database to update.</param>
        /// <param name="database">The updated database entity.</param>
        /// <returns>The number of affected rows.</returns>
        [HttpPut("{id}")]
        public async Task<ActionResult<int>> UpdateDatabase(int id, [FromBody] Database database)
        {
            if (id != database.DatabaseID)
                return BadRequest("Database ID mismatch.");

            var result = await _service.UpdateDatabaseAsync(database);
            return Ok(result);
        }

        /// <summary>
        /// Deletes a database.
        /// </summary>
        /// <param name="id">The ID of the database to delete.</param>
        /// <returns>The number of affected rows.</returns>
        [HttpDelete("{id}")]
        public async Task<ActionResult<int>> DeleteDatabase(int id)
        {
            var result = await _service.DeleteDatabaseAsync(id);
            return Ok(result);
        }

        /// <summary>
        /// Tests the connection to a database.
        /// </summary>
        /// <param name="database">The database entity to test the connection for.</param>
        /// <returns>True if the connection is successful, otherwise false.</returns>
        [HttpPost("test-connection")]
        public async Task<ActionResult<bool>> TestConnection([FromBody] Database database)
        {
            var result = await _service.TestConnectionAsync(database);
            return Ok(result);
        }
    }
}