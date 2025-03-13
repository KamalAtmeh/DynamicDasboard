using System;
using System.Threading.Tasks;
using DynamicDashboardCommon.Enums;
using DynamicDashboardCommon.Models;
using DynamicDasboardWebAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace DynamicDasboardWebAPI.Controllers
{
    [ApiController]
    [Route("api/schema-analysis")]
    public class SchemaAnalysisController : AppControllerBase
    {
        private readonly SchemaAnalysisService _analysisService;

        public SchemaAnalysisController(
            SchemaAnalysisService analysisService,
            ILogsService logsService)
            : base(logsService)
        {
            _analysisService = analysisService ?? throw new ArgumentNullException(nameof(analysisService));
        }

        /// <summary>
        /// Analyzes database schema using LLM to generate descriptions and identify conflicts
        /// </summary>
        /// <param name="databaseId">The ID of the database to analyze</param>
        /// <returns>Schema analysis result</returns>
        [HttpGet("analyze/{databaseId}")]
        public async Task<IActionResult> AnalyzeDatabaseSchema(int databaseId)
        {
            try
            {
                var result = await _analysisService.AnalyzeDatabaseSchemaAsync(databaseId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, EnumLoggingType.Error.ToString());
            }
        }

        /// <summary>
        /// Applies schema analysis results to update database schema metadata
        /// </summary>
        /// <param name="databaseId">The ID of the database</param>
        /// <param name="analysisData">The analysis data to apply</param>
        /// <returns>Success indicator</returns>
        [HttpPost("apply/{databaseId}")]
        public async Task<IActionResult> ApplySchemaAnalysisResults(int databaseId, [FromBody] SchemaAnalysisData analysisData)
        {
            try
            {
                var result = await _analysisService.ApplySchemaAnalysisResultsAsync(databaseId, analysisData);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, EnumLoggingType.Error.ToString());
            }
        }
    }
}