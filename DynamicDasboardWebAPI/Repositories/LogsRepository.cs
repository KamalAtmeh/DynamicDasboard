using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using DynamicDasboardWebAPI.Utilities;

namespace DynamicDasboardWebAPI.Repositories
{
    /// <summary>
    /// Repository for logging events in the Logs table.
    /// </summary>
    public class LogsRepository
    {
        private readonly IDbConnection _connection;
        private readonly ILogger<LogsRepository> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="LogsRepository"/> class.
        /// </summary>
        /// <param name="connection">Database connection instance.</param>
        /// <param name="logger">Optional logger for capturing repository operations.</param>
        public LogsRepository(IDbConnection connection, ILogger<LogsRepository> logger = null)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _logger = logger;
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
            try
            {
                if (string.IsNullOrWhiteSpace(eventType))
                    throw new ArgumentException("Event type cannot be empty", nameof(eventType));

                if (string.IsNullOrWhiteSpace(eventDescription))
                    throw new ArgumentException("Event description cannot be empty", nameof(eventDescription));

                string query = @"
                    INSERT INTO Logs (UserID, EventType, EventDescription, Timestamp)
                    VALUES (@UserID, @EventType, @EventDescription, GETDATE())";

                return await _connection.ExecuteSafeAsync(query, new
                {
                    UserID = userId,
                    EventType = eventType,
                    EventDescription = eventDescription
                });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error adding log: {EventType}, {EventDescription}", eventType, eventDescription);
                throw;
            }
        }

        /// <summary>
        /// Gets recent log entries.
        /// </summary>
        /// <param name="limit">Maximum number of log entries to retrieve.</param>
        /// <returns>A collection of recent log entries.</returns>
        public async Task<IEnumerable<dynamic>> GetRecentLogsAsync(int limit = 100)
        {
            try
            {
                string query = @"
                    SELECT TOP (@Limit) *
                    FROM Logs
                    ORDER BY Timestamp DESC";

                return await _connection.QuerySafeAsync<dynamic>(query, new { Limit = limit });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving recent logs");
                throw;
            }
        }

        /// <summary>
        /// Gets log entries by event type.
        /// </summary>
        /// <param name="eventType">Type of event to filter by.</param>
        /// <param name="limit">Maximum number of log entries to retrieve.</param>
        /// <returns>A collection of log entries of the specified type.</returns>
        public async Task<IEnumerable<dynamic>> GetLogsByEventTypeAsync(string eventType, int limit = 100)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(eventType))
                    throw new ArgumentException("Event type cannot be empty", nameof(eventType));

                string query = @"
                    SELECT TOP (@Limit) *
                    FROM Logs
                    WHERE EventType = @EventType
                    ORDER BY Timestamp DESC";

                return await _connection.QuerySafeAsync<dynamic>(query, new { EventType = eventType, Limit = limit });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving logs by event type: {EventType}", eventType);
                throw;
            }
        }

        /// <summary>
        /// Gets log entries for a specific user.
        /// </summary>
        /// <param name="userId">User ID to filter by.</param>
        /// <param name="limit">Maximum number of log entries to retrieve.</param>
        /// <returns>A collection of log entries for the specified user.</returns>
        public async Task<IEnumerable<dynamic>> GetLogsByUserIdAsync(int userId, int limit = 100)
        {
            try
            {
                string query = @"
                    SELECT TOP (@Limit) *
                    FROM Logs
                    WHERE UserID = @UserID
                    ORDER BY Timestamp DESC";

                return await _connection.QuerySafeAsync<dynamic>(query, new { UserID = userId, Limit = limit });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving logs for user: {UserID}", userId);
                throw;
            }
        }
    }
}