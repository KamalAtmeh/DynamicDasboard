using System.Threading.Tasks;
using DynamicDasboardWebAPI.Repositories;
using DynamicDashboardCommon.Models;

namespace DynamicDasboardWebAPI.Services
{
    /// <summary>
    /// Service layer for logging events.
    /// </summary>


    public class LogsService : ILogsService
    {
        private readonly LogsRepository _repository;

        /// <summary>
        /// Initializes a new instance of the <see cref="LogsService"/> class.
        /// </summary>
        /// <param name="repository">Instance of the logs repository.</param>
        public LogsService(LogsRepository repository)
        {
            _repository = repository;
        }

        /// <summary>
        /// Logs an exception into the database.
        /// </summary>
        /// <param name="userId">The ID of the user (nullable).</param>
        /// <param name="eventType">The type of the event.</param>
        /// <param name="eventDescription">The description of the event.</param>
        public async Task LogExceptionAsync(int? userId, string eventType, string eventDescription)
        {
            await _repository.AddLogAsync(userId, eventType, eventDescription);
        }
    }
}
