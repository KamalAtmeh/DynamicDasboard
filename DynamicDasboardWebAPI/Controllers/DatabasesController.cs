using DynamicDasboardWebAPI.Services;
using DynamicDashboardCommon.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DynamicDasboardWebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DatabasesController : ControllerBase
    {
        private readonly DatabaseService _service;
        private readonly ILogger<DatabasesController> _logger;

        public DatabasesController(DatabaseService service, ILogger<DatabasesController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Database>>> GetAllDatabases()
        {
            var databases = await _service.GetAllDatabasesAsync();
            return Ok(databases);
        }

        [HttpGet("{databaseID}")]
        public async Task<ActionResult<IEnumerable<Database>>> GetDataBaseByID(int databaseID)
        {
            var databases = await _service.GetDatabaseByIdAsync(databaseID);
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
            if (database == null)
            {
                return BadRequest("Connection details are required.");
            }
            try
            {
                var result = await _service.TestConnectionAsync(database);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing database connection.");
                return false;
            }
        }

        [HttpGet("types")]
        public async Task<ActionResult<IEnumerable<(int TypeId, string TypeName)>>> GetDatabaseTypes()
        {
            try
            {
                var types = await _service.GetAllDatabaseTypesAsync();
                return Ok(types);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving database types");
                return StatusCode(500, "An error occurred while retrieving database types.");
            }
        }

        [HttpGet("type/{typeId}")]
        public async Task<ActionResult<string>> GetDatabaseTypeName(int typeId)
        {
            try
            {
                var typeName = await _service.GetDatabaseTypeNameAsync(typeId);
                return Ok(typeName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving database type name for ID {typeId}");
                return StatusCode(500, ex.Message + " An error occurred while retrieving database type name.");
            }
        }

        // Add to existing DatabasesController class
        [HttpGet("{id}/schema")]
        public async Task<ActionResult<IEnumerable<SchemaTableDto>>> GetDatabaseSchema(int id)
        {
            try
            {
                var schema = await _service.RetrieveDatabaseSchemaAsync(id);
                return Ok(schema);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving schema for database ID {DatabaseId}", id);
                return StatusCode(500, "An error occurred while retrieving the database schema.");
            }
        }

        [HttpPost("{id}/schema")]
        public async Task<ActionResult> SaveDatabaseSchema(int id, [FromBody] IEnumerable<SchemaTableDto> schema)
        {
            try
            {
                await _service.SaveDatabaseSchemaAsync(id, schema);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving schema for database ID {DatabaseId}", id);
                return StatusCode(500, "An error occurred while saving the database schema.");
            }
        }

    }
}