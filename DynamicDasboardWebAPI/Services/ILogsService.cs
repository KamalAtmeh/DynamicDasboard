using System.Threading.Tasks;
using DynamicDasboardWebAPI.Repositories;


namespace DynamicDasboardWebAPI.Services
{
    /// <summary>
    /// Provides methods for logging exceptions.
    /// </summary>
    public interface ILogsService
    {
        /// <summary>
        /// Logs an exception asynchronously.
        /// </summary>
        /// <param name="userId">The ID of the user who encountered the exception. Can be null.</param>
        /// <param name="eventType">The type of event that occurred.</param>
        /// <param name="eventDescription">A description of the event.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task LogExceptionAsync(int? userId, string eventType, string eventDescription);
    }
}
