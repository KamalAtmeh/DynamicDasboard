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
        public async Task<ActionResult<ConnectionTestResult>> TestConnection([FromBody] ConnectionTestRequest request)
        {
            if (request == null)
            {
                return BadRequest("Connection details are required.");
            }

            try
            {
                var result = await _service.TestConnectionAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing database connection.");
                return StatusCode(500, new ConnectionTestResult
                {
                    Success = false,
                    Message = $"Error: {ex.Message}",
                    ErrorDetails = ex.ToString()
                });
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
                return StatusCode(500, "An error occurred while retrieving database type name.");
            }
        }

    }
}