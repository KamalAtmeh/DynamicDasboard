using Microsoft.AspNetCore.Http;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using DynamicDasboardWebAPI.Services;

namespace DynamicDasboardWebAPI.Utilities
{
    /// <summary>
    /// Middleware to handle exceptions globally and log them using ILogsService.
    /// </summary>
    public class CustomExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomExceptionMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        public CustomExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        /// <summary>
        /// Invokes the middleware to handle exceptions.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <returns>A task that represents the completion of request processing.</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                // Resolve ILogsService from the scoped service provider
                var logsService = context.RequestServices.GetRequiredService<DynamicDasboardWebAPI.Services.ILogsService>();
                await logsService.LogExceptionAsync(
                    userId: null, // Optional: Fetch from context
                    eventType: "Error",
                    eventDescription: ex.Message
                );

                // Handle the exception and return a response to the client
                await HandleExceptionAsync(context, ex);
            }
        }

        /// <summary>
        /// Handles the exception and writes a JSON response to the client.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <param name="exception">The exception that occurred.</param>
        /// <returns>A task that represents the completion of response writing.</returns>
        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var response = new
            {
                StatusCode = context.Response.StatusCode,
                Message = "An unexpected error occurred. Please try again later.",
                Details = exception.InnerException
            };

            return context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}
