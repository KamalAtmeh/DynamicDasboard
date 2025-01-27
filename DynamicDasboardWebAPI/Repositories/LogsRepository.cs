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
        /// <remarks>
        /// This method logs an event in the database by inserting a new record into the Logs table.
        /// The log entry includes the user ID (if available), the type of event, and a description of the event.
        /// The current timestamp is automatically added to the log entry.
        /// </remarks>
        public async Task<int> AddLogAsync(int? userId, string eventType, string eventDescription)
        {
            // SQL query to insert a new log entry into the Logs table
            string query = @"
                INSERT INTO Logs (UserID, EventType, EventDescription, Timestamp)
                VALUES (@UserID, @EventType, @EventDescription, GETDATE())";

            // Execute the query asynchronously and return the number of rows affected
            return await _connection.ExecuteAsync(query, new { UserID = userId, EventType = eventType, EventDescription = eventDescription });
        }
    }
}
