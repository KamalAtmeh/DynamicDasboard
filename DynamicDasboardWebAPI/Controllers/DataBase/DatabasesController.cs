using DynamicDasboardWebAPI.Services;
using DynamicDashboardCommon.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly;
using DynamicDashboardCommon.Enums;

namespace DynamicDasboardWebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DatabasesController : AppControllerBase
    {
        private readonly DatabaseService _service;


        public DatabasesController(DatabaseService service,
        ILogsService logsService)
        : base(logsService)
        {
            _service = service;
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
                return await HandleExceptionAsync(ex, LoggingType.Error.ToString());
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
                return await HandleExceptionAsync(ex, LoggingType.Error.ToString());

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
                return await HandleExceptionAsync(ex, LoggingType.Error.ToString());
              
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
                return await HandleExceptionAsync(ex, LoggingType.Error.ToString());
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
                return await HandleExceptionAsync(ex, LoggingType.Error.ToString());
            }
        }

        [HttpGet("{id}/relationships")]
        public async Task<ActionResult<IEnumerable<SchemaRelationshipDto>>> GetDatabaseRelationships(int id)
        {
            try
            {
                var relationships = await _service.GetDatabaseRelationshipsAsync(id);
                return Ok(relationships);
            }
            catch (Exception ex)
            {
               return await HandleExceptionAsync(ex, LoggingType.Error.ToString());
            }
        }

    }
}