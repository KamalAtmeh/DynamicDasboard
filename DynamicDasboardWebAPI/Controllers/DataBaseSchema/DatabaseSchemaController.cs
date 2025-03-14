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
        private readonly DatabaseService _databaseService;

        public DatabaseSchemaController(
            DatabaseSchemaService service, DatabaseService databaseService, ILogsService logsService)
            : base(logsService)
        {
            _dbSchemaService = service;
            _databaseService = databaseService;
        }

        /// <summary>
        /// Create a new schema entry.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateSchema([FromBody] DatabaseSchema schema)
        {
            try
            {
                var id = await _dbSchemaService.CreateSchemaAsync(schema);
                return Ok(id);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, EnumLoggingType.Error.ToString());
            }
        }

        /// <summary>
        /// Update an existing schema entry.
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSchema(int id, [FromBody] DatabaseSchema schema)
        {
            if (id != schema.ID)
                return BadRequest("Schema ID mismatch.");

            try
            {
                var result = await _dbSchemaService.UpdateSchemaAsync(schema);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, EnumLoggingType.Error.ToString());
            }
        }

        /// <summary>
        /// Retrieve a schema entry by its ID.
        /// </summary>
        [HttpGet("GetSchema/{databaseID}")]
        public async Task<IActionResult> GetSchemaByDataBaseID(int databaseID)
        {
            try
            {
                var schema = await _dbSchemaService.GetSchemaByDataBaseIdAsync(databaseID);
                if (schema == null || schema.ID == 0)
                {
                    return null; //temp Service need to handle the return and validations
                }
                return Ok(schema);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, EnumLoggingType.Error.ToString());
            }
        }

        /// <summary>
        /// Deactivate (soft-delete) a schema entry by updating its status.
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeactivateSchemaByDataBaseID(int id)
        {
            try
            {
                var result = await _dbSchemaService.DeactivateSchemaAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, EnumLoggingType.Error.ToString());
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
                return await HandleExceptionAsync(ex, EnumLoggingType.Error.ToString());
            }
        }

        /// <summary>
        /// Refreshes the database schema while preserving metadata
        /// </summary>
        [HttpPost("refresh/{databaseId}")]
        public async Task<IActionResult> RefreshDatabaseSchema(int databaseId)
        {
            try
            {
                var database = await _databaseService.GetDatabaseByIdAsync(databaseId);
                if (database == null)
                    return NotFound("Database not found");

                var schema = await _dbSchemaService.RefreshAndGetDatabaseSchemaFromConnectedDBAsync(databaseId, database);
                return Ok(schema);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, EnumLoggingType.Error.ToString());
            }
        }

        /// <summary>
        /// Updates the active status of a table
        /// </summary>
        [HttpPut("tables/{databaseId}/{tableId}/active")]
        public async Task<IActionResult> UpdateTableActiveStatus(int databaseId, string tableId, [FromBody] bool isActive)
        {
            try
            {
                var result = await _dbSchemaService.UpdateTableActiveStatusAsync(databaseId, tableId, isActive);
                if (!result)
                    return NotFound("Table not found");

                return Ok(result);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, EnumLoggingType.Error.ToString());
            }
        }

        /// <summary>
        /// Updates the active status of a column
        /// </summary>
        [HttpPut("columns/{databaseId}/{tableId}/{columnId}/active")]
        public async Task<IActionResult> UpdateColumnActiveStatus(int databaseId, string tableId, string columnId, [FromBody] bool isActive)
        {
            try
            {
                var result = await _dbSchemaService.UpdateColumnActiveStatusAsync(databaseId, tableId, columnId, isActive);
                if (!result)
                    return NotFound("Column not found");

                return Ok(result);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, EnumLoggingType.Error.ToString());
            }
        }

        /// <summary>
        /// Updates the active status of a relationship
        /// </summary>
        [HttpPut("relationships/{databaseId}/{relationshipId}/active")]
        public async Task<IActionResult> UpdateRelationshipActiveStatus(int databaseId, string relationshipId, [FromBody] bool isActive)
        {
            try
            {
                var result = await _dbSchemaService.UpdateRelationshipActiveStatusAsync(databaseId, relationshipId, isActive);
                if (!result)
                    return NotFound("Relationship not found");

                return Ok(result);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, EnumLoggingType.Error.ToString());
            }
        }

        #endregion


        #region Json Schema CRUD Operations



        // Get parsed schema
        [HttpGet("parsed/{databaseID}")]
        public async Task<IActionResult> GetParsedSchema(int databaseID)
        {
            try
            {
                var schema = await _dbSchemaService.GetSchemaByDataBaseIdAsync(databaseID);
                if (schema == null)
                    return NotFound();

                var schemaDetail = _dbSchemaService.DeserializeSchema(schema.SchemaData);
                return Ok(schemaDetail);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, EnumLoggingType.Error.ToString());
            }
        }
        #endregion
    }
}