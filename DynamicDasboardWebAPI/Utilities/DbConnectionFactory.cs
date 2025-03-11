using DynamicDashboardCommon.Enums;
using DynamicDashboardCommon.Models;
using Dapper;
using Microsoft.Data.SqlClient;
using MySql.Data.MySqlClient;
using Oracle.ManagedDataAccess.Client;
using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Collections.Generic;

#region NAMESPACE_AND_DEPENDENCIES
namespace DynamicDasboardWebAPI.Utilities
{
    #endregion

    public class DbConnectionFactory
    {
        #region FIELDS

        private readonly IDbConnection _appDbConnection;
        private readonly IConfiguration _configuration;
        private readonly ConcurrentDictionary<int, string> _connectionStringCache;
        private readonly ConcurrentDictionary<int, string> _databaseTypeCache;

        #endregion

        #region CONSTRUCTOR

        public DbConnectionFactory(
            IDbConnection appDbConnection,
            IConfiguration configuration)
        {
            _appDbConnection = appDbConnection;
            _configuration = configuration;

            _connectionStringCache = new ConcurrentDictionary<int, string>();
            _databaseTypeCache = new ConcurrentDictionary<int, string>();
        }

        #endregion

        #region CREATE_CONNECTION_METHODS

        /// <summary>
        /// Creates a database connection based on the database ID.
        /// </summary>
        /// //Temp to change it into async
        public IDbConnection CreateConnection(int databaseId)
        {
            IDbConnection connection = null;
            try
            {
                var (connectionString, databaseType) = GetConnectionInfo(databaseId);

                connection = BuildConnection((EnumDatabaseType)databaseType, connectionString);

                if (connection is DbConnection dbConnection)
                {
                    dbConnection.Open();
                }
                else
                {
                    connection.Open();
                }

                return connection;
            }
            catch (Exception)
            {
                connection?.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Creates and opens a database connection asynchronously.
        /// </summary>
        public async Task<IDbConnection> CreateOpenConnectionAsync(int databaseId)
        {
            var (connectionString, databaseType) = GetConnectionInfo(databaseId);

            if (string.IsNullOrWhiteSpace(connectionString))
                return null;

            IDbConnection connection = null;

            try
            {
                connection = BuildConnection((EnumDatabaseType)databaseType, connectionString);

                if (connection is DbConnection dbConnection)
                {
                    await dbConnection.OpenAsync();
                }
                else
                {
                    // Dapper + IDbConnection doesn't have native async open
                    await Task.Run(() => connection.Open());
                }

                return connection;
            }
            catch
            {
                connection?.Dispose();
                throw;
            }
        }

        #endregion

        #region TEST_CONNECTION_METHODS

        /// <summary>
        /// Tests a database connection asynchronously.
        /// </summary>
        public async Task<bool> TestConnectionAsync(int databaseId)
        {
            try
            {
                using var connection = await CreateOpenConnectionAsync(databaseId);
                return true;
            }
            catch(Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Tests a database connection using explicit connection parameters.
        /// </summary>
        public async Task<bool> TestConnectionAsync(Database database, string connectionString)
        {
            if (database == null)
                return false;

            int DBType = database.TypeID;
            try
            {
                if (string.IsNullOrEmpty(connectionString))
                {
                    connectionString = BuildConnectionString(database);
                }

                IDbConnection connection = BuildConnection((EnumDatabaseType)DBType, connectionString);

                // Check if connection is closed before opening it
                if (connection.State != ConnectionState.Open)
                {
                    if (connection is DbConnection dbConnection)
                    {
                        await dbConnection.OpenAsync();
                    }
                    else
                    {
                        await Task.Run(() => connection.Open());
                    }
                }

                return true;
            }
            catch
            {
                throw;
            }
        }

        #endregion

        #region BUILD_CONNECTION_AND_STRING_METHODS

        /// <summary>
        /// Builds a connection string from a Database object
        /// </summary>
        public string BuildConnectionString(Database database)
        {
            if (database == null)
                return string.Empty;

            // Use existing connection string if provided
            if (!string.IsNullOrWhiteSpace(database.ConnectionString))
                return database.ConnectionString;

            // Build the connection string based on database type
            return database.TypeID switch
            {
                (int)EnumDatabaseType.SQLServer => BuildSqlServerConnectionString(database),
                (int)EnumDatabaseType.MySQL => BuildMySqlConnectionString(database),
                (int)EnumDatabaseType.Oracle => BuildOracleConnectionString(database),
                _ => string.Empty
            };
        }

        public IDbConnection BuildConnection(EnumDatabaseType dbType, string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                return null;
            }

            return dbType switch
            {
                EnumDatabaseType.SQLServer => new SqlConnection(connectionString),
                EnumDatabaseType.MySQL => new MySqlConnection(connectionString),
                EnumDatabaseType.Oracle => new OracleConnection(connectionString),
                _ => null
            };
        }

        #endregion

        #region CACHE_METHODS

        /// <summary>
        /// Clears the connection string cache
        /// </summary>
        public void ClearCache()
        {
            _connectionStringCache.Clear();
            _databaseTypeCache.Clear();
        }

        #endregion

        #region EXECUTE_WITH_CONNECTION_METHODS

        /// <summary>
        /// Executes an operation with proper connection management for a specific database
        /// </summary>
        public async Task<T> ExecuteWithConnectionAsync<T>(
            int databaseId,
            Func<IDbConnection, Task<T>> operation,
            int retryCount = 3,
            int initialDelayMs = 1000)
        {
            if (operation == null)
            {
                return default;
            }

            // Instead of throwing an exception, log or handle if the databaseId is invalid.
            if (databaseId <= 0)
            {
                return default;
            }

            Exception lastException = null;
            int delay = initialDelayMs;

            for (int i = 0; i < retryCount; i++)
            {
                IDbConnection connection = null;

                try
                {
                    // Create and open connection
                    connection = await CreateOpenConnectionAsync(databaseId);

                    // Execute the operation
                    T result = await operation(connection);

                    // Update connection status to successful
                    try
                    {
                        await UpdateConnectionStatusAsync(databaseId, true);
                    }
                    catch
                    {
                        // Logging or rethrow can occur here
                        throw;
                    }

                    return result;
                }
                catch (Exception ex)
                {
                    lastException = ex;

                    if (i < retryCount - 1)
                    {
                        await Task.Delay(delay);
                        delay *= 2; // Exponential backoff
                    }
                }
                finally
                {
                    if (connection != null && connection.State != ConnectionState.Closed)
                    {
                        try
                        {
                            connection.Close();
                            (connection as IDisposable)?.Dispose();
                        }
                        catch
                        {
                            // Logging or rethrow can occur here
                            throw;
                        }
                    }
                }
            }

            // Update connection status to failed.
            try
            {
                await UpdateConnectionStatusAsync(databaseId, false);
            }
            catch
            {
                // Logging or rethrow
                throw;
            }

            // Here, lastException could be logged or rethrown. Returning default for now.
            return default;
        }

        /// <summary>
        /// Executes an operation with proper connection management for a specific database (void return)
        /// </summary>
        public async Task ExecuteWithConnectionAsync(
            int databaseId,
            Func<IDbConnection, Task> operation,
            int retryCount = 3,
            int initialDelayMs = 1000)
        {
            await ExecuteWithConnectionAsync<object>(
                databaseId,
                async (conn) =>
                {
                    await operation(conn);
                    return null;
                },
                retryCount,
                initialDelayMs);
        }

        /// <summary>
        /// Executes an operation with proper connection management using the application database connection
        /// </summary>
        public async Task<T> ExecuteWithAppConnectionAsync<T>(Func<IDbConnection, Task<T>> operation)
        {
            if (operation == null) return default;

            bool wasOpen = _appDbConnection.State == ConnectionState.Open;

            try
            {
                if (!wasOpen)
                    _appDbConnection.Open();

                return await operation(_appDbConnection);
            }
            finally
            {
                if (!wasOpen && _appDbConnection.State == ConnectionState.Open)
                    _appDbConnection.Close();
            }
        }

        /// <summary>
        /// Executes an operation with proper connection management using the application database connection (void return)
        /// </summary>
        public async Task ExecuteWithAppConnectionAsync(Func<IDbConnection, Task> operation)
        {
            if (operation == null)
                return;

            bool wasOpen = _appDbConnection.State == ConnectionState.Open;

            try
            {
                if (!wasOpen)
                    _appDbConnection.Open();

                await operation(_appDbConnection);
            }
            finally
            {
                if (!wasOpen && _appDbConnection.State == ConnectionState.Open)
                    _appDbConnection.Close();
            }
        }

        #endregion

        #region PRIVATE_HELPERS

        /// <summary>
        /// Gets connection information for a database by ID
        /// </summary>
        /// <param name="databaseId">The database ID</param>
        /// <returns>A tuple containing connection string and database type ID</returns>
        private (string ConnectionString, int DatabaseTypeId) GetConnectionInfo(int databaseId)
        {
            if (databaseId <= 0)
                return (null, 0);

            string connectionString = string.Empty;
            int databaseTypeId = 0;

            try
            {
                // Check if connection string is in cache
                bool connectionStringCached = _connectionStringCache.TryGetValue(databaseId, out connectionString);
                bool databaseTypeCached = _databaseTypeCache.TryGetValue(databaseId, out string databaseTypeStr);

                // If both are cached, return them
                if (connectionStringCached && databaseTypeCached && int.TryParse(databaseTypeStr, out databaseTypeId))
                {
                    return (connectionString, databaseTypeId);
                }

                // At least one value is not cached, fetch database info
                string query = @"
                    SELECT d.*, dt.TypeName as DatabaseTypeName 
                    FROM Databases d 
                    LEFT JOIN DatabaseTypes dt ON d.TypeID = dt.TypeID 
                    WHERE d.DatabaseID = @DatabaseID AND d.IsActive = 1";

                var database = _appDbConnection.QueryFirstOrDefault<Database>(query, new { DatabaseID = databaseId });
                if (database == null)
                    return (null, 0);

                // Update connection string if not cached
                if (!connectionStringCached)
                {
                    connectionString = database.ConnectionString ?? BuildConnectionString(database);
                    _connectionStringCache.TryAdd(databaseId, connectionString);
                }

                // Update database type if not cached
                if (!databaseTypeCached)
                {
                    databaseTypeId = database.TypeID;
                    _databaseTypeCache.TryAdd(databaseId, databaseTypeId.ToString());
                }
                else if (!int.TryParse(databaseTypeStr, out databaseTypeId))
                {
                    // If cached value exists but couldn't be parsed as int
                    databaseTypeId = database.TypeID;
                    _databaseTypeCache.TryAdd(databaseId, databaseTypeId.ToString());
                }
            }
            catch
            {
                throw;
            }

            return (connectionString, databaseTypeId);
        }

        private string BuildSqlServerConnectionString(Database database)
        {
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = database.ServerAddress,
                InitialCatalog = database.DatabaseName,
                ConnectTimeout = 30
                // MultipleActiveResultSets = true // if needed
            };

            // If credentials are encrypted, decrypt them
            string decryptedPassword = DecryptCredentials(database.EncryptedCredentials);

            if (string.IsNullOrEmpty(database.Username))
            {
                builder.IntegratedSecurity = true;
            }
            else
            {
                builder.UserID = database.Username;
                builder.Password = decryptedPassword;
            }

            return builder.ConnectionString;
        }

        private string BuildMySqlConnectionString(Database database)
        {
            var builder = new MySqlConnectionStringBuilder
            {
                Server = database.ServerAddress,
                Database = database.DatabaseName,
                Port = (uint)database.Port,
                ConnectionTimeout = 30
            };

            // If credentials are encrypted, decrypt them
            string decryptedPassword = DecryptCredentials(database.EncryptedCredentials);

            if (database.Username != null)
            {
                builder.UserID = database.Username;
                builder.Password = decryptedPassword;
            }

            return builder.ConnectionString;
        }

        private string BuildOracleConnectionString(Database database)
        {
            var builder = new OracleConnectionStringBuilder
            {
                DataSource = database.ServerAddress,
                ConnectionTimeout = 30
            };

            // If credentials are encrypted, decrypt them
            string decryptedPassword = DecryptCredentials(database.EncryptedCredentials);

            if (database.Username != null)
            {
                builder.UserID = database.Username;
                builder.Password = decryptedPassword;
            }

            return builder.ConnectionString;
        }

        private string DecryptCredentials(string encryptedCredentials)
        {
            if (string.IsNullOrEmpty(encryptedCredentials))
                return string.Empty;

            try
            {
                // TODO: Implement actual decryption logic
                // In production, you should use a secure decryption method
                // For example: return _cryptoService.Decrypt(encryptedCredentials);
                return encryptedCredentials; // Placeholder for now
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Updates the connection status for a database
        /// </summary>
        private async Task UpdateConnectionStatusAsync(int databaseId, bool status)
        {
            await ExecuteWithAppConnectionAsync(async conn =>
            {
                await conn.ExecuteAsync(@"
                    UPDATE Databases 
                    SET LastConnectionStatus = @Status, 
                        LastTransactionDate = GETDATE()
                    WHERE DatabaseID = @DatabaseID",
                    new { Status = status, DatabaseID = databaseId });
            });
        }

        #endregion
    }

}
