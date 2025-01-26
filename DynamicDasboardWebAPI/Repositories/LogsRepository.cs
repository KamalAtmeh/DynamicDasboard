using System.Data;
using System.Threading.Tasks;
using Dapper;
using DynamicDashboardCommon.Models;

namespace DynamicDasboardWebAPI.Repositories
{
    /// <summary>
    /// Repository for logging events in the Logs table.
    /// </summary>
    public class LogsRepository
    {
        private readonly IDbConnection _connection;

        /// <summary>
        /// Initializes a new instance of the <see cref="LogsRepository"/> class.
        /// </summary>
        /// <param name="connection">Database connection instance.</param>
        public LogsRepository(IDbConnection connection)
        {
            _connection = connection;
        }

        /// <summary>
        /// Inserts a new log entry into the Logs table.
        /// </summary>
        /// <param name="userId">The ID of the user (nullable).</param>
        /// <param name="eventType">The type of the event.</param>
        /// <param name="eventDescription">The description of the event.</param>
        /// <returns>The number of rows affected.</returns>
        public async Task<int> AddLogAsync(int? userId, string eventType, string eventDescription)
        {
            string query = @"
                INSERT INTO Logs (UserID, EventType, EventDescription, Timestamp)
                VALUES (@UserID, @EventType, @EventDescription, GETDATE())";

            return await _connection.ExecuteAsync(query, new { UserID = 1, EventType = eventType, EventDescription = eventDescription });
        }
    }
}
