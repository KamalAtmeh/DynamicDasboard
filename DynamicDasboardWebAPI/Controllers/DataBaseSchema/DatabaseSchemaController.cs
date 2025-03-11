using System;
using System.Threading.Tasks;
using DynamicDashboardCommon.Models;
using DynamicDasboardWebAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using DynamicDashboardCommon.Enums;

namespace DynamicDasboardWebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DatabaseSchemaController : AppControllerBase
    {
        private readonly DatabaseSchemaService _dbSchemaService;

        public DatabaseSchemaController(
            DatabaseSchemaService service,
            ILogsService logsService)
            : base(logsService)
        {
            _dbSchemaService = service;
        }

        /// <summary>
        /// Create a new schema entry.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateSchema([FromBody] DatabaseJsonSchema schema)
        {
            try
            {
                var id = await _dbSchemaService.CreateSchemaAsync(schema);
                return Ok(id);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, LoggingType.Error.ToString());
            }
        }

        /// <summary>
        /// Update an existing schema entry.
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSchema(int id, [FromBody] DatabaseJsonSchema schema)
        {
            if (id != schema.Id)
                return BadRequest("Schema ID mismatch.");

            try
            {
                var result = await _dbSchemaService.UpdateSchemaAsync(schema);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, LoggingType.Error.ToString());
            }
        }

        /// <summary>
        /// Retrieve a schema entry by its ID.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetSchema(int id)
        {
            try
            {
                var schema = await _dbSchemaService.GetSchemaByIdAsync(id);
                if (schema == null)
                    return NotFound();

                return Ok(schema);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, LoggingType.Error.ToString());
            }
        }

        /// <summary>
        /// Deactivate (soft-delete) a schema entry by updating its status.
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeactivateSchema(int id)
        {
            try
            {
                var result = await _dbSchemaService.DeactivateSchemaAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, LoggingType.Error.ToString());
            }
        }

        #region Schema Analysis

        /// <summary>
        /// Placeholder endpoint for analyzing a database schema.
        /// </summary>
        [HttpGet("analyze/{databaseId}")]
        public async Task<IActionResult> AnalyzeDatabaseSchema(int databaseId)
        {
            try
            {
                // Example placeholder logic:
                // var analysisResult = await _dbSchemaService.AnalyzeSchemaAsync(databaseId);
                // return Ok(analysisResult);

                // Currently returns a simple BadRequest as in your snippet:
                return BadRequest(string.Empty);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, LoggingType.Error.ToString());
            }
        }

        #endregion
    }
}