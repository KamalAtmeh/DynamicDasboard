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
    public class DatabaseSchemaController : ControllerBase
    {
        private readonly DatabaseSchemaService _DBSchemaService;

        public DatabaseSchemaController(DatabaseSchemaService service)
        {
            _DBSchemaService = service;
            
        }

        // Create a new schema entry
        [HttpPost]
        public async Task<ActionResult<int>> CreateSchema([FromBody] DatabaseJsonSchema schema)
        {
            try
            {
                var id = await _DBSchemaService.CreateSchemaAsync(schema);
                return Ok(id);
            }
            catch (Exception ex)
            {
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
                var result = await _DBSchemaService.UpdateSchemaAsync(schema);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal server error.");
            }
        }

        // Retrieve a schema entry by its ID
        [HttpGet("{id}")]
        public async Task<ActionResult<DatabaseJsonSchema>> GetSchema(int id)
        {
            try
            {
                var schema = await _DBSchemaService.GetSchemaByIdAsync(id);
                if (schema == null)
                    return NotFound();

                return Ok(schema);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal server error.");
            }
        }

        // Deactivate (soft-delete) a schema entry by updating its status
        [HttpDelete("{id}")]
        public async Task<ActionResult<int>> DeactivateSchema(int id)
        {
            try
            {
                var result = await _DBSchemaService.DeactivateSchemaAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal server error.");
            }
        }


        #region Schema Analysis

        [HttpGet("analyze/{databaseId}")]
        public async Task<IActionResult> AnalyzeDatabaseSchema(int databaseId)
        {
            return BadRequest(string.Empty);
        }


        #endregion


    }
}
