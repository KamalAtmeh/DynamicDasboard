using DynamicDashboardCommon.Models;
using Microsoft.Data.SqlClient;
using MySql.Data.MySqlClient;
using Oracle.ManagedDataAccess.Client;
using System.Collections.Concurrent;
using System.Data.Common;
using System.Data;
using DynamicDashboardCommon.Enums;
using Dapper;
using System.Collections.Generic;

public class DbConnectionFactory
{
    private readonly IDbConnection _appDbConnection;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DbConnectionFactory> _logger;
    private readonly ConcurrentDictionary<int, string> _connectionStringCache;
    private readonly ConcurrentDictionary<int, string> _databaseTypeCache;

    public DbConnectionFactory(
        IDbConnection appDbConnection,
        IConfiguration configuration,
        ILogger<DbConnectionFactory> logger = null)
    {
        _appDbConnection = appDbConnection ?? throw new ArgumentNullException(nameof(appDbConnection));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger;
        _connectionStringCache = new ConcurrentDictionary<int, string>();
        _databaseTypeCache = new ConcurrentDictionary<int, string>();
    }

    /// <summary>
    /// Creates a database connection based on the database ID.
    /// </summary>
    public IDbConnection CreateConnection(int databaseId)
    {
        var (connectionString, databaseType) = GetConnectionInfo(databaseId);

        return databaseType switch
        {
            (int)(DatabaseTypeEnum.SQLServer) => new SqlConnection(connectionString),
            (int)(DatabaseTypeEnum.MySQL) => new MySqlConnection(connectionString),
            (int)(DatabaseTypeEnum.Oracle) => new OracleConnection(connectionString),
            _ => throw new ArgumentException($"Unsupported database type: {databaseType}")
        };
    }

    /// <summary>
    /// Creates and opens a database connection asynchronously.
    /// </summary>
    public async Task<IDbConnection> CreateOpenConnectionAsync(int databaseId)
    {
        var (connectionString, databaseType) = GetConnectionInfo(databaseId);

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string is missing or invalid.");

        IDbConnection connection = null;

        try
        {
             connection = databaseType switch
            {
                (int)(DatabaseTypeEnum.SQLServer) => new SqlConnection(connectionString),
                (int)(DatabaseTypeEnum.MySQL) => new MySqlConnection(connectionString),
                (int)(DatabaseTypeEnum.Oracle) => new OracleConnection(connectionString),
                _ => throw new ArgumentException($"Unsupported database type: {databaseType}")
            };

            if (connection is DbConnection dbConnection)
            {
                await dbConnection.OpenAsync();
            }
            else
            {
                connection.Open();
            }

            return connection;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to open connection for database ID: {DatabaseId}", databaseId);
            connection?.Dispose();
            throw;
        }
    }

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
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Connection test failed for database ID: {DatabaseId}", databaseId);
            return false;
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

            using IDbConnection connection = database.TypeID switch
            {
                (int)(DatabaseTypeEnum.SQLServer) => new SqlConnection(connectionString),
                (int)(DatabaseTypeEnum.MySQL) => new MySqlConnection(connectionString),
                (int)(DatabaseTypeEnum.Oracle) => new OracleConnection(connectionString),
                _ => throw new ArgumentException($"Unsupported database type: {database.TypeID}")
            };

            // Check if connection is closed before opening it
            if (connection.State != ConnectionState.Open)
            {
                if (connection is DbConnection dbConnection)
                {
                    await dbConnection.OpenAsync();
                }
                else
                {
                    connection.Open();
                }
            }

            return true;
    }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Connection test failed for database: {Name}", database.Name);
            return false;
        }
    }

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

    // Get the database type name and use it to build the connection string
    

    return database.TypeID switch
    {
        (int)DatabaseTypeEnum.SQLServer => BuildSqlServerConnectionString(database),
        (int)DatabaseTypeEnum.MySQL => BuildMySqlConnectionString(database),
        (int)DatabaseTypeEnum.Oracle => BuildOracleConnectionString(database)
    };
}

/// <summary>
/// Clears the connection string cache
/// </summary>
public void ClearCache()
{
    _connectionStringCache.Clear();
    _databaseTypeCache.Clear();
}

    // Private helper methods for getting connection info and building connection strings
    /// <summary>
    /// Gets connection information for a database by ID
    /// </summary>
    /// <param name="databaseId">The database ID</param>
    /// <returns>A tuple containing connection string and database type ID</returns>
    private (string ConnectionString, int DatabaseTypeId) GetConnectionInfo(int databaseId)
    {
        if (databaseId <= 0)
            throw new ArgumentException("Invalid database ID", nameof(databaseId));

        string connectionString = null;
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
                throw new ArgumentException($"Database with ID {databaseId} not found or inactive");

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
                _databaseTypeCache.TryAdd(databaseId, databaseTypeId.ToString()); // Override with correct value
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error retrieving connection info for database ID {DatabaseId}", databaseId);

            // Rethrow as a more specific exception
            if (ex is ArgumentException)
            {
                throw; // Keep original exception
            }
            else
            {
                throw new DatabaseException($"Failed to get connection information for database ID {databaseId}", ex);
            }
        }

        // Final validation
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new DatabaseException($"Could not determine connection string for database ID {databaseId}");
        }

        if (databaseTypeId <= 0)
        {
            throw new DatabaseException($"Could not determine database type for database ID {databaseId}");
        }

        return (connectionString, databaseTypeId);
    }

// Connection string builder methods
private string BuildSqlServerConnectionString(Database database)
{
    var builder = new SqlConnectionStringBuilder
    {
        DataSource = database.ServerAddress,
        InitialCatalog = database.DatabaseName,
        ConnectTimeout = 30
    };

    // If credentials are encrypted, decrypt them
    string decryptedPassword = DecryptCredentials(database.EncryptedCredentials);

    if (database.Username == null)
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
        Port = database.Port.HasValue ? (uint)database.Port.Value : 3306u,
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
    catch (Exception ex)
    {
        _logger?.LogError(ex, "Error decrypting credentials");
        return string.Empty;
    }
}
}