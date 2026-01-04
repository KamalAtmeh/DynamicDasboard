using DynamicDashboardCommon.Enums;
using DynamicDashboardCommon.Models;
using DynamicDasboardWebAPI.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace DynamicDasboardWebAPI.Controllers
{
    /// <summary>
    /// API controller for dashboard AI assistant operations
    /// </summary>
    [ApiController]
    [Route("api/assistant")]
    public class DashboardAssistantController : AppControllerBase
    {
        private readonly IAssistantService _assistantService;

        /// <summary>
        /// Initializes a new instance of the <see cref="DashboardAssistantController"/> class.
        /// </summary>
        public DashboardAssistantController(
            IAssistantService assistantService,
            ILogsService logsService)
            : base(logsService)
        {
            _assistantService = assistantService ?? throw new ArgumentNullException(nameof(assistantService));
        }

        /// <summary>
        /// Generates smart suggestions for dashboard improvements
        /// </summary>
        /// <param name="request">Request containing dashboard and components info</param>
        /// <returns>List of suggested components</returns>
        [HttpPost("suggestions")]
        public async Task<IActionResult> GetSuggestions([FromBody] AssistantChatRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new { message = "Request cannot be null" });
                }

                if (request.DashboardId <= 0)
                {
                    return BadRequest(new { message = "Valid dashboard ID is required" });
                }

                var response = await _assistantService.GenerateSuggestionsAsync(request);
                
                if (!response.Success)
                {
                    return StatusCode(500, new { message = response.Message });
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex, EnumLoggingType.Error.ToString());
            }
        }
    }
}
