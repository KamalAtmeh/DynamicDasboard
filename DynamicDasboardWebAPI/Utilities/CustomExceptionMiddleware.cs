using Microsoft.AspNetCore.Http;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using DynamicDasboardWebAPI.Services;

namespace DynamicDasboardWebAPI.Utilities
{
    public class CustomExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        public CustomExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

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

                await HandleExceptionAsync(context, ex);
            }
        }

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
