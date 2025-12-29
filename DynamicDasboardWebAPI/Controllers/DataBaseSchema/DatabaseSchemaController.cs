using System;
using System.Threading.Tasks;
using DynamicDashboardCommon.Models;
using DynamicDasboardWebAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using DynamicDashboardCommon.Enums;
using DynamicDashboardCommon.Helper;
using System.Text.Json;


namespace DynamicDasboardWebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DatabaseSchemaController : AppControllerBase
    {
        private readonly DatabaseSchemaService objDBSchemaService;
        private readonly DatabaseService _databaseService;

        public DatabaseSchemaController(
            DatabaseSchemaService service, DatabaseService databaseService, ILogsService logsService)
            : base(logsService)
        {
            objDBSchemaService = service;
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
                var id = await objDBSchemaService.CreateSchemaAsync(schema);
                return Ok(id);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, EnumLoggingType.Error.ToString());
            }
        }

        /// <summary>
        /// Update an existing schema entry.
        /// Accepts both schema ID (primary key) and database ID (foreign key)
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSchema(int id, [FromBody] DatabaseSchema schema)
        {
            try
            {
                // Check if id is the schema ID or database ID
                // First, try to find by DatabaseID (most common from frontend)
                var existingSchema = await objDBSchemaService.GetSchemaWithJsonByDataBaseIdAsync(id);

                if (existingSchema != null)
                {
                    // id is the DatabaseID - use existing schema's actual ID
                    schema.ID = existingSchema.ID;
                    schema.DataBaseID = id;
                }
                else if (id == schema.ID)
                {
                    // id matches schema.ID - proceed normally
                }
                else
                {
                    return BadRequest("Schema not found for the provided ID.");
                }

                // Ensure ModifiedAt is set
                schema.ModifiedAt = DateTime.UtcNow;

                var result = await objDBSchemaService.UpdateSchemaAsync(schema);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, EnumLoggingType.Error.ToString());
            }
        }


        /// <summary>
        /// Update schema by DatabaseID (for frontend convenience)
        /// </summary>
        //[HttpPut("ByDatabaseId/{databaseId}")]
        //public async Task<IActionResult> UpdateSchemaByDatabaseId(int databaseId, [FromBody] DatabaseSchema schema)
        //{
        //    try
        //    {
        //        // Ensure the DataBaseID matches the route parameter
        //        schema.DataBaseID = databaseId;

        //        // Get existing schema to get the actual ID
        //        var existingSchema = await objDBSchemaService.GetSchemaWithJsonByDataBaseIdAsync(databaseId);
        //        if (existingSchema == null)
        //            return NotFound($"No schema found for database ID {databaseId}");

        //        // Copy the actual database row ID
        //        schema.ID = existingSchema.ID;
        //        schema.ModifiedAt = DateTime.UtcNow;

        //        var result = await objDBSchemaService.UpdateSchemaAsync(schema);
        //        return Ok(result);
        //    }
        //    catch (Exception ex)
        //    {
        //        return await HandleExceptionAsync(ex, EnumLoggingType.Error.ToString());
        //    }
        //}

        /// <summary>
        /// Update schema by DatabaseID - designed for frontend use
        /// Bypasses automatic model validation for partial updates
        /// </summary>
        [HttpPut("UpdateByDatabaseId/{databaseId}")]
        public async Task<IActionResult> UpdateSchemaByDatabaseId(int databaseId, [FromBody] JsonElement schemaJson)
        {
            try
            {
                // Get existing schema to find the actual row ID
                var existingSchema = await objDBSchemaService.GetSchemaWithJsonByDataBaseIdAsync(databaseId);

                if (existingSchema == null)
                    return NotFound($"No schema found for database ID {databaseId}");

                // Extract SchemaData from the incoming JSON
                string newSchemaData = null;

                if (schemaJson.TryGetProperty("SchemaData", out var schemaDataProp) ||
                    schemaJson.TryGetProperty("schemaData", out schemaDataProp))
                {
                    newSchemaData = schemaDataProp.GetString();
                }

                if (string.IsNullOrEmpty(newSchemaData))
                {
                    return BadRequest("SchemaData is required");
                }

                // Update only the SchemaData field, preserve everything else
                existingSchema.SchemaData = newSchemaData;
                existingSchema.ModifiedAt = DateTime.UtcNow;

                var result = await objDBSchemaService.UpdateSchemaAsync(existingSchema);

                // Invalidate cache
                await CacheHelper.InvalidateDatabaseCacheAsync(databaseId);

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
                var schema = await objDBSchemaService.GetSchemaWithJsonByDataBaseIdAsync(databaseID);
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
                var result = await objDBSchemaService.DeactivateSchemaAsync(id);
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

                var schema = await objDBSchemaService.RefreshAndGetDatabaseSchemaFromConnectedDBAsync(databaseId, database);
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
                var result = await objDBSchemaService.UpdateTableActiveStatusAsync(databaseId, tableId, isActive);
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
                var result = await objDBSchemaService.UpdateColumnActiveStatusAsync(databaseId, tableId, columnId, isActive);
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
                var result = await objDBSchemaService.UpdateRelationshipActiveStatusAsync(databaseId, relationshipId, isActive);
                if (!result)
                    return NotFound("Relationship not found");

                return Ok(result);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, EnumLoggingType.Error.ToString());
            }
        }

        // In DatabaseSchemaController.cs
        [HttpPut("UpdateTableDetailsByTableID/{databaseId}/{tableId}")]
        public async Task<IActionResult> UpdateTableDetailsByTableID(int databaseId, string tableId, [FromBody] TableSchema tableUpdate)
        {
            try
            {
                var result = await objDBSchemaService.UpdateTableDetailsByTableID(databaseId, tableId, tableUpdate);
                if (!result)
                    return NotFound("Table not found");

                return Ok(result);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, EnumLoggingType.Error.ToString());
            }
        }

        [HttpPut("UpdateColumnsDetailsByColumnID/{databaseId}/{tableId}")]
        public async Task<IActionResult> UpdateColumnsDetailsByColumnID(int databaseId, string tableId, [FromBody] List<ColumnSchema> lstColumns)
        {
            try
            {
                var result = await objDBSchemaService.UpdateColumnsDetailsByColumnID(databaseId, tableId, lstColumns);
                if (!result)
                    return NotFound("Table not found");

                return Ok(result);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, EnumLoggingType.Error.ToString());
            }
        }

        //TODO Similar endpoints for columns and relationships
        [HttpGet("TableListBasicInfo/{databaseID}")]
        public async Task<IActionResult> GetSchemaBasicTablesList(int databaseID)
        {
            try
            {
                var lstTableBasicInfo = await objDBSchemaService.GetSchemaBasicTablesList(databaseID);
                if (lstTableBasicInfo == null || lstTableBasicInfo.Count == 0)
                {
                    return null;
                }
                return Ok(lstTableBasicInfo);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, EnumLoggingType.Error.ToString());
            }
        }

       
        [HttpGet("GetTableDetails/{databaseID}/{tableID}")]
        public async Task<IActionResult> GetSchemaTableDetailsByID(int databaseID,string tableID)
        {
            try
            {
                var objSchemaTable = await objDBSchemaService.GetSchemaTableDetailsByID(databaseID, tableID);
                if (objSchemaTable == null)
                {
                    return null;
                }
                return Ok(objSchemaTable);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, EnumLoggingType.Error.ToString());
            }
        }

        [HttpPost("{databaseId}/termMappings")]
        public async Task<IActionResult> SaveTermMappings(int databaseId, [FromBody] List<TermMapping> termMappings)
        {
            try
            {
                var result = await objDBSchemaService.SaveTermMappingsAsync(databaseId, termMappings);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, EnumLoggingType.Error.ToString());
            }
        }

        #endregion


        // Get parsed schema
        [HttpGet("parsed/{databaseID}/{useCache}")]
        public async Task<IActionResult> GetSchemaDeserialized(int databaseID, int useCache)
        {
            try
            {
                object objSchemaDetail = null;
                if (useCache == 0)
                {
                     objSchemaDetail = await objDBSchemaService.GetSchemaObject(databaseID, false);
                }
                else
                {
                    objSchemaDetail = await objDBSchemaService.GetSchemaObject(databaseID);
                }
                return Ok(objSchemaDetail);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, EnumLoggingType.Error.ToString());
            }
        }

        [HttpGet("OptimizedSchemaString/{databaseID}")]
        public async Task<IActionResult> BuildOptimizedSchemaString(int databaseID)
        { 
            DatabaseSchema objSchema = await objDBSchemaService.GetSchemaObject(databaseID);

            string strOptimizedSchema =  objDBSchemaService.BuildOptimizedSchemaString(objSchema);

            return Ok(strOptimizedSchema);
        }

        [HttpPost("cache/clear/{databaseId}")]
        public async Task<IActionResult> ClearDatabaseCache(int databaseId)
        {
            await CacheHelper.InvalidateDatabaseCacheAsync(databaseId);
            return Ok(new { Message = $"Cache cleared for database {databaseId}" });
        }

        /// <summary>
        /// Clear all cache
        /// </summary>
        [HttpPost("cache/clearall")]
        public async Task<IActionResult> ClearAllCache()
        {
            await CacheHelper.ClearAsync();
            return Ok(new { Message = "All cache cleared" });
        }

        /// <summary>
        /// Update schema by DatabaseID - bypasses strict model validation for partial updates
        /// </summary>
        //[HttpPut("ByDatabaseId/{databaseId}")]
        //public async Task<IActionResult> UpdateSchemaByDatabaseId(int databaseId, [FromBody] JsonElement schemaJson)
        //{
        //    try
        //    {
        //        // Get existing schema to preserve required fields
        //        var existingSchema = await objDBSchemaService.GetSchemaWithJsonByDataBaseIdAsync(databaseId);
        //        if (existingSchema == null)
        //            return NotFound($"No schema found for database ID {databaseId}");

        //        // Deserialize the incoming JSON to get the SchemaData
        //        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        //        string newSchemaData;

        //        // Check if SchemaData is provided in the request
        //        if (schemaJson.TryGetProperty("SchemaData", out var schemaDataElement) ||
        //            schemaJson.TryGetProperty("schemaData", out schemaDataElement))
        //        {
        //            newSchemaData = schemaDataElement.GetString();
        //        }
        //        else
        //        {
        //            // The entire JSON is the schema data
        //            newSchemaData = schemaJson.GetRawText();
        //        }

        //        // Update only the SchemaData field
        //        existingSchema.SchemaData = newSchemaData;
        //        existingSchema.ModifiedAt = DateTime.UtcNow;

        //        var result = await objDBSchemaService.UpdateSchemaAsync(existingSchema);

        //        // Invalidate cache for this database
        //        await CacheHelper.InvalidateDatabaseCacheAsync(databaseId);

        //        return Ok(result);
        //    }
        //    catch (Exception ex)
        //    {
        //        return await HandleExceptionAsync(ex, EnumLoggingType.Error.ToString());
        //    }
        //}


    }
}