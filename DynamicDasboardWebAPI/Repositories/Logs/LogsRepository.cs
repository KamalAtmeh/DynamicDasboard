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
    public class LogsRepository : BaseRepository
    {
       

        /// <summary>
        /// Initializes a new instance of the <see cref="LogsRepository"/> class.
        /// </summary>
        /// <param name="appDbConnection">Default database connection instance.</param>
        /// <param name="connectionFactory">Factory for dynamic connections (not used here).</param>
        /// <param name="logger">Optional logger for capturing repository operations.</param>
        public LogsRepository(
            IDbConnection appDbConnection,
            DbConnectionFactory connectionFactory)
            : base(appDbConnection, connectionFactory)
        {

        }

        /// <summary>
        /// Inserts a new log entry into the Logs table (default DB).
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

                const string query = @"
                    INSERT INTO Logs (UserID, EventType, EventDescription, Timestamp)
                    VALUES (@UserID, @EventType, @EventDescription, GETDATE())";

                return await WithConnectionAsync(async conn =>
                {
                    return await conn.ExecuteSafeAsync(query, new
                    {
                        UserID = userId,
                        EventType = eventType,
                        EventDescription = eventDescription
                    });
                });
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Gets recent log entries (default DB).
        /// </summary>
        /// <param name="limit">Maximum number of log entries to retrieve.</param>
        public async Task<IEnumerable<dynamic>> GetRecentLogsAsync(int limit = 100)
        {
            try
            {
                const string query = @"
                    SELECT TOP (@Limit) *
                    FROM Logs
                    ORDER BY Timestamp DESC";

                return await WithConnectionAsync(async conn =>
                {
                    return await conn.QuerySafeAsync<dynamic>(query, new { Limit = limit });
                });
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Gets log entries by event type (default DB).
        /// </summary>
        /// <param name="eventType">Type of event to filter by.</param>
        /// <param name="limit">Maximum number of log entries to retrieve.</param>
        public async Task<IEnumerable<dynamic>> GetLogsByEventTypeAsync(string eventType, int limit = 100)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(eventType))
                    throw new ArgumentException("Event type cannot be empty", nameof(eventType));

                const string query = @"
                    SELECT TOP (@Limit) *
                    FROM Logs
                    WHERE EventType = @EventType
                    ORDER BY Timestamp DESC";

                return await WithConnectionAsync(async conn =>
                {
                    return await conn.QuerySafeAsync<dynamic>(query, new
                    {
                        EventType = eventType,
                        Limit = limit
                    });
                });
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Gets log entries for a specific user (default DB).
        /// </summary>
        /// <param name="userId">User ID to filter by.</param>
        /// <param name="limit">Maximum number of log entries to retrieve.</param>
        public async Task<IEnumerable<dynamic>> GetLogsByUserIdAsync(int userId, int limit = 100)
        {
            try
            {
                const string query = @"
                    SELECT TOP (@Limit) *
                    FROM Logs
                    WHERE UserID = @UserID
                    ORDER BY Timestamp DESC";

                return await WithConnectionAsync(async conn =>
                {
                    return await conn.QuerySafeAsync<dynamic>(query, new
                    {
                        UserID = userId,
                        Limit = limit
                    });
                });
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}