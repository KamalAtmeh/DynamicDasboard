using DynamicDashboardCommon.Models;
using DynamicDasboardWebAPI.Services.LLM;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace DynamicDasboardWebAPI.Controllers.Test
{
    [ApiController]
    [Route("api/[controller]")]
    public class LLMTestController : ControllerBase
    {
        private readonly ILLMService _llmService;

        public LLMTestController(LLMServiceFactory llmServiceFactory)
        {
            _llmService = llmServiceFactory.CreateLlmService();
        }

        [HttpGet("LLMTestForSQLGeneration")]
        public async Task<IActionResult> TestSqlGeneration()
        {
            try
            {
                string question = "What are the top 5 customers by revenue?";
                string schema = "Tables: [Customers(CustomerId, Name, Email), Orders(OrderId, CustomerId, OrderDate, TotalAmount)]";

                var result = await _llmService.GenerateSqlWithExplanationAsync(question, schema);

                return Ok(new
                {
                    Question = question,
                    SqlQuery = result.SqlQuery,
                    Explanation = result.BusinessExplanation
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }
    }
}