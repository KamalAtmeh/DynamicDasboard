using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using DynamicDasboardWebAPI.Services;
using Microsoft.Extensions.Logging;
using DynamicDashboardCommon.Models;

namespace DynamicDasboardWebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SchemaAnalysisController : ControllerBase
    {
        private readonly SchemaAnalysisService _schemaAnalysisService;
        private readonly ILogger<SchemaAnalysisController> _logger;

        public SchemaAnalysisController(
            SchemaAnalysisService schemaAnalysisService,
            ILogger<SchemaAnalysisController> logger)
        {
            _schemaAnalysisService = schemaAnalysisService;
            _logger = logger;
        }

        [HttpGet("analyze/{databaseId}")]
        public async Task<IActionResult> AnalyzeDatabaseSchema(int databaseId)
        {
            try
            {
                var result = await _schemaAnalysisService.AnalyzeDatabaseSchemaAsync(databaseId);

                if (result.Success)
                {
                    return Ok(result.AnalysisData);
                }
                else
                {
                    return BadRequest(result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing database schema");
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        [HttpPost("apply-descriptions/{databaseId}")]
        public async Task<IActionResult> ApplyDescriptions(int databaseId, [FromBody] SchemaAnalysisData analysisData)
        {
            try
            {
                var result = await _schemaAnalysisService.ApplyDescriptionsAsync(databaseId, analysisData);

                if (result)
                {
                    return Ok(new { success = true, message = "Descriptions applied successfully" });
                }
                else
                {
                    return BadRequest("Failed to apply descriptions");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying descriptions");
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        [HttpPost("add-relationship/{databaseId}")]
        public async Task<IActionResult> AddSuggestedRelationship(
            int databaseId,
            [FromBody] SuggestedRelationship relationshipData)
        {
            try
            {
                var result = await _schemaAnalysisService.AddSuggestedRelationshipAsync(databaseId, relationshipData);

                if (result)
                {
                    return Ok(new { success = true, message = "Relationship added successfully" });
                }
                else
                {
                    return BadRequest("Failed to add relationship");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding relationship");
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }
    }
}