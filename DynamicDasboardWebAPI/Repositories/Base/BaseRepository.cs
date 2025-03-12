using DynamicDasboardWebAPI.Utilities;
using System;
using System.Data;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace DynamicDasboardWebAPI.Repositories

{
    public abstract class BaseRepository
    {
        protected readonly IDbConnection _appDbConnection;      // The "default" application DB
        protected readonly DbConnectionFactory _connectionFactory; // For dynamic connections

        protected BaseRepository(IDbConnection appDbConnection, DbConnectionFactory connectionFactory)
        {
            _appDbConnection = appDbConnection;
            _connectionFactory = connectionFactory;
        }

        /// <summary>
        /// A common helper to manage connection creation, opening, and disposal 
        /// for a specific database ID.
        /// </summary>
        protected async Task<T> WithConnectionAsync<T>(
            Func<IDbConnection, Task<T>> operation, int databaseId = 0, bool isApplicationDB = false)
        {
            try
            {
                // If the caller specified a valid DB ID, use DbConnectionFactory
                if (databaseId > 0)
                {
                    // Create & open a dynamic connection for the given ID
                    using var conn = await _connectionFactory.CreateOpenConnectionAsync(databaseId);
                    return await operation(conn);
                }
                else
                {
                    // Otherwise, use the default (application) DB connection
                    // Make sure we open/close it only if we opened it ourselves
                    bool wasClosed = _appDbConnection.State == ConnectionState.Closed;
                    if (wasClosed)
                        _appDbConnection.Open();

                    try
                    {
                        return await operation(_appDbConnection);
                    }
                    finally
                    {
                        // Close if we opened it in this scope
                        if (wasClosed && _appDbConnection.State == ConnectionState.Open)
                            _appDbConnection.Close();
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }


        /// <summary>
        /// Overload for void-returning operations
        /// </summary>
        protected async Task WithConnectionAsync(
            Func<IDbConnection, Task> operation, int databaseId = 0)
        {
            try
            {
                if (databaseId > 0)
                {
                    using var conn = await _connectionFactory.CreateOpenConnectionAsync(databaseId);
                    await operation(conn);
                }
                else
                {
                    bool wasClosed = _appDbConnection.State == ConnectionState.Closed;
                    if (wasClosed)
                        _appDbConnection.Open();

                    try
                    {
                        await operation(_appDbConnection);
                    }
                    finally
                    {
                        if (wasClosed && _appDbConnection.State == ConnectionState.Open)
                            _appDbConnection.Close();
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

    }
}
