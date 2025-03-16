using System;
using System.Threading.Tasks;
using DynamicDashboardCommon.Enums;
using DynamicDashboardCommon.Models;
using DynamicDasboardWebAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace DynamicDasboardWebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SchemaAnalysisController : AppControllerBase
    {
        private readonly SchemaAnalysisService objAnalysisService;

        public SchemaAnalysisController(
            SchemaAnalysisService analysisService,
            ILogsService logsService)
            : base(logsService)
        {
            objAnalysisService = analysisService ?? throw new ArgumentNullException(nameof(analysisService));
        }

        /// <summary>
        /// Analyzes database schema using LLM to generate descriptions and identify conflicts
        /// </summary>
        /// <param name="databaseId">The ID of the database to analyze</param>
        /// <returns>Schema analysis result</returns>
        [HttpGet("AnalyzeDatabaseSchema/{databaseId}")]
        public async Task<IActionResult> AnalyzeDatabaseSchema(int databaseId)
        {
            try
            {
                var result = await objAnalysisService.AnalyzeDatabaseSchemaAsync(databaseId);
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
        [HttpPost("ApplySchemaAnalysisResults/{databaseId}")]
        public async Task<IActionResult> ApplySchemaAnalysisResults(int databaseId, [FromBody] SchemaAnalysisData analysisData)
        {
            try
            {
                var result = await objAnalysisService.ApplySchemaAnalysisResultsAsync(databaseId, analysisData);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, EnumLoggingType.Error.ToString());
            }
        }

        // In DynamicDasboardWebAPI/Controllers/DataBaseSchema/DatabaseSchemaController.cs
        [HttpGet("SuggestTerms/{databaseId}")]
        public async Task<IActionResult> SuggestTermMappings(int databaseId)
        {
            try
            {
                var suggestions = await objAnalysisService.SuggestTermMappingsAsync(databaseId);
                return Ok(suggestions);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, EnumLoggingType.Error.ToString());
            }
        }



        // Add to SchemaAnalysisController.cs

        [HttpPost("analyze-tables")]
        public async Task<IActionResult> AnalyzeTablesOnly([FromBody] SchemaAnalysisRequest request)
        {
            try
            {
                var result = await objAnalysisService.AnalyzeTablesAsync(request.DatabaseId, request.SchemaString);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, EnumLoggingType.Error.ToString());
            }
        }

        [HttpPost("analyze-columns")]
        public async Task<IActionResult> AnalyzeColumnsOnly([FromBody] SchemaAnalysisRequest request)
        {
            try
            {
                var result = await objAnalysisService.AnalyzeColumnsAsync(request.DatabaseId, request.SchemaString);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, EnumLoggingType.Error.ToString());
            }
        }

        [HttpPost("analyze-relationships")]
        public async Task<IActionResult> AnalyzeRelationshipsOnly([FromBody] SchemaAnalysisRequest request)
        {
            try
            {
                var result = await objAnalysisService.AnalyzeRelationshipsAsync(request.DatabaseId, request.SchemaString);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, EnumLoggingType.Error.ToString());
            }
        }

        [HttpPost("analyze-conflicts")]
        public async Task<IActionResult> AnalyzeConflictsOnly([FromBody] SchemaAnalysisRequest request)
        {
            try
            {
                var result = await objAnalysisService.AnalyzeConflictsAsync(request.DatabaseId, request.SchemaString);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, EnumLoggingType.Error.ToString());
            }
        }

        [HttpPost("analyze-term-mappings")]
        public async Task<IActionResult> AnalyzeTermMappings([FromBody] SchemaAnalysisRequest request)
        {
            try
            {
                var result = await objAnalysisService.GenerateTermMappingsAsync(request.DatabaseId, request.SchemaString);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, EnumLoggingType.Error.ToString());
            }
        }
    }
}