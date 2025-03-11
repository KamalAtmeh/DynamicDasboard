using System;
using System.Security.Claims;
using System.Threading.Tasks;
using DynamicDasboardWebAPI.Services;
using DynamicDashboardCommon.Enums;
using DynamicDashboardCommon.Helper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace DynamicDasboardWebAPI.Controllers
{
    /// <summary>
    /// Base controller that provides logging and common functionality for all API controllers.
    /// </summary>
    public abstract class AppControllerBase : ControllerBase
    {

        private readonly ILogsService _logsService;

        /// <summary>
        /// Initializes a new instance of the <see cref="AppControllerBase"/> class.
        /// </summary>
        /// <param name="logsService">The primary logging service.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        protected AppControllerBase(
            ILogsService logsService)
        {
            _logsService = logsService;
        }

        /// <summary>
        /// Gets the current user ID from claims, or null if not authenticated, to be moved
        /// </summary>
        /// <returns>The user ID or null.</returns>
        protected int? GetUserId()
        {

            //Temp
            //if (!User.Identity.IsAuthenticated)
            //    return null;

            //var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            //if (int.TryParse(userIdClaim, out int userId))
            //    return userId;

            //return null;

            return 1;
        }

        /// <summary>
        /// Logs an exception and returns a standardized error response.
        /// </summary>
        /// <param name="ex">The exception to log.</param>
        /// <param name="eventType">The type of event that caused the exception.</param>
        /// <param name="customMessage">Optional custom message to return to the client.</param>
        /// <returns>A 500 Internal Server Error response.</returns>
        protected async Task<ActionResult> HandleExceptionAsync(
            Exception ex,
            string eventType,
            string customMessage = null)
        {
    
            await _logsService.AddLogAsync(GetUserId(), eventType, ApplicationHelper.GetExceptionDetails(ex));

            return StatusCode(DetermineStatusCode(ex), new
            {
                Message = customMessage ?? "An unexpected error occurred. Please try again later.",
                Timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Handles a not found result with logging.
        /// </summary>
        /// <param name="entityName">The name of the entity that wasn't found.</param>
        /// <param name="id">The ID that was searched for.</param>
        /// <returns>A 404 Not Found response.</returns>
        protected async Task<IActionResult> HandleNotFoundAsync(string entityName, object id)
        {
            var message = $"{entityName} with ID {id} not found";
            await _logsService.AddLogAsync(GetUserId(), Enum.GetName(typeof(LoggingType), LoggingType.Warning), message);

            return NotFound(new
            {
                Message = message,
                Timestamp = DateTime.UtcNow
            });
        }

        private int DetermineStatusCode(Exception ex)
        {
            return ex switch
            {
                ArgumentException or ArgumentNullException or FormatException
                    => 400, // Bad Request

                UnauthorizedAccessException
                    => 401, // Unauthorized

                KeyNotFoundException or FileNotFoundException
                    => 404, // Not Found

                TimeoutException
                    => 408, // Request Timeout

                NotImplementedException
                    => 501, // Not Implemented

                // Default case
                _ => 500 // Internal Server Error
            };
        }
    }
}