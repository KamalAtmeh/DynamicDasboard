using System;
using System.Threading.Tasks;
using DynamicDashboardCommon.Models;
using DynamicDasboardWebAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DynamicDasboardWebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DatabaseJsonSchemaController : ControllerBase
    {
        private readonly DatabaseJsonSchemaService _service;
        private readonly ILogger<DatabaseJsonSchemaController> _logger;

        public DatabaseJsonSchemaController(DatabaseJsonSchemaService service, ILogger<DatabaseJsonSchemaController> logger)
        {
            _service = service;
            _logger = logger;
        }

        // Create a new schema entry
        [HttpPost]
        public async Task<ActionResult<int>> CreateSchema([FromBody] DatabaseJsonSchema schema)
        {
            try
            {
                var id = await _service.CreateSchemaAsync(schema);
                return Ok(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating schema.");
                return StatusCode(500, "Internal server error.");
            }
        }

        // Update an existing schema entry
        [HttpPut("{id}")]
        public async Task<ActionResult<int>> UpdateSchema(int id, [FromBody] DatabaseJsonSchema schema)
        {
            if (id != schema.Id)
                return BadRequest("Schema ID mismatch.");

            try
            {
                var result = await _service.UpdateSchemaAsync(schema);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating schema.");
                return StatusCode(500, "Internal server error.");
            }
        }

        // Retrieve a schema entry by its ID
        [HttpGet("{id}")]
        public async Task<ActionResult<DatabaseJsonSchema>> GetSchema(int id)
        {
            try
            {
                var schema = await _service.GetSchemaByIdAsync(id);
                if (schema == null)
                    return NotFound();

                return Ok(schema);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving schema.");
                return StatusCode(500, "Internal server error.");
            }
        }

        // Deactivate (soft-delete) a schema entry by updating its status
        [HttpDelete("{id}")]
        public async Task<ActionResult<int>> DeactivateSchema(int id)
        {
            try
            {
                var result = await _service.DeactivateSchemaAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating schema.");
                return StatusCode(500, "Internal server error.");
            }
        }
    }
}
