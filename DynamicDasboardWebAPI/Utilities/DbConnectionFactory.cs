using System;
using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Oracle.ManagedDataAccess.Client;
using DynamicDashboardCommon.Models;
using DynamicDasboardWebAPI.Utilities;

public class DbConnectionFactory
{
    private readonly IDbConnection _appDbConnection;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DbConnectionFactory> _logger;
    private readonly ConcurrentDictionary<string, string> _connectionCache;

    public DbConnectionFactory(
        IDbConnection appDbConnection,
        IConfiguration configuration,
        ILogger<DbConnectionFactory> logger = null)
    {
        _appDbConnection = appDbConnection;
        _configuration = configuration;
        _logger = logger;
        _connectionCache = new ConcurrentDictionary<string, string>();
    }

    /// <summary>
    /// Creates a database connection based on the database ID or database type.
    /// </summary>
    /// <param name="databaseIdOrType">Database ID (numeric) or database type name</param>
    /// <returns>An IDbConnection instance.</returns>
    public IDbConnection CreateConnection(string databaseIdOrType)
    {
        string connectionString = GetConnectionString(databaseIdOrType);
        string dbType = GetDatabaseType(databaseIdOrType);

        return dbType.ToLowerInvariant() switch
        {
            "sqlserver" => new SqlConnection(connectionString),
            "mysql" => new MySqlConnection(connectionString),
            "oracle" => new OracleConnection(connectionString),
            _ => throw new ArgumentException($"Unsupported database type: {dbType}")
        };
    }

    /// <summary>
    /// Creates and opens a database connection asynchronously based on database ID or type.
    /// </summary>
    /// <param name="databaseIdOrType">Database ID (numeric) or database type name</param>
    /// <returns>An opened IDbConnection instance.</returns>
    public async Task<IDbConnection> CreateOpenConnectionAsync(string databaseIdOrType)
    {
        string connectionString = GetConnectionString(databaseIdOrType);
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string is missing or invalid.");

        string dbType = GetDatabaseType(databaseIdOrType);
        IDbConnection connection = null;

        try
        {
            connection = dbType.ToLowerInvariant() switch
            {
                "sqlserver" => new SqlConnection(connectionString),
                "mysql" => new MySqlConnection(connectionString),
                "oracle" => new OracleConnection(connectionString),
                _ => throw new ArgumentException($"Unsupported database type: {dbType}")
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
            _logger?.LogError(ex, "Failed to open connection for database: {DatabaseIdOrType}", databaseIdOrType);
            connection?.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Tests a database connection asynchronously.
    /// </summary>
    /// <param name="databaseIdOrType">Database ID or type name.</param>
    /// <returns>True if connection successful; otherwise, false.</returns>
    public async Task<bool> TestConnectionAsync(string databaseIdOrType)
    {
        try
        {
            using var connection = await CreateOpenConnectionAsync(databaseIdOrType);
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Connection test failed for {DatabaseIdOrType}", databaseIdOrType);
            return false;
        }
    }

    /// <summary>
    /// Tests a database connection using explicit connection parameters.
    /// </summary>
    /// <param name="database">Database object with connection details</param>
    /// <returns>True if connection successful; otherwise, false.</returns>
    public async Task<bool> TestConnectionAsync(Database database)
    {
        if (database == null)
            throw new ArgumentNullException(nameof(database));

        try
        {
            string connectionString = BuildConnectionString(database);
            string dbType = GetDatabaseTypeNameFromId(database.TypeID);

            using IDbConnection connection = dbType.ToLowerInvariant() switch
            {
                "sqlserver" => new SqlConnection(connectionString),
                "mysql" => new MySqlConnection(connectionString),
                "oracle" => new OracleConnection(connectionString),
                _ => throw new ArgumentException($"Unsupported database type: {dbType}")
            };

            if (connection is DbConnection dbConnection)
            {
                await dbConnection.OpenAsync();
            }
            else
            {
                connection.Open();
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
            throw new ArgumentNullException(nameof(database));

        // Use existing connection string if provided
        if (!string.IsNullOrWhiteSpace(database.ConnectionString))
            return database.ConnectionString;

        string dbType = GetDatabaseTypeNameFromId(database.TypeID).ToLowerInvariant();

        return dbType switch
        {
            "sqlserver" => BuildSqlServerConnectionString(database),
            "mysql" => BuildMySqlConnectionString(database),
            "oracle" => BuildOracleConnectionString(database),
            _ => throw new ArgumentException($"Unsupported database type: {dbType}")
        };
    }

     public async Task<T> ExecuteWithConnectionAsync<T>(string databaseIdOrType, Func<IDbConnection, Task<T>> operation)
    {
        using var connection = await CreateOpenConnectionAsync(databaseIdOrType);
        return await operation(connection);
    }
    
    public async Task<IEnumerable<T>> QueryAsync<T>(string databaseIdOrType, string sql, object parameters = null)
    {
        return await ExecuteWithConnectionAsync(databaseIdOrType, async (connection) => 
            await connection.QuerySafeAsync<T>(sql, parameters));
    }
    
    public async Task<T> QueryFirstOrDefaultAsync<T>(string databaseIdOrType, string sql, object parameters = null)
    {
        return await ExecuteWithConnectionAsync(databaseIdOrType, async (connection) => 
            await connection.QueryFirstOrDefaultSafeAsync<T>(sql, parameters));
    }

    /// <summary>
    /// Gets the connection string for a database by ID or type.
    /// If numeric, treats as database ID and fetches from DB.
    /// If string, treats as type name and uses cached value or fetches from DB.
    /// </summary>
    private string GetConnectionString(string databaseIdOrType)
    {
        // Check if value is in cache
        if (_connectionCache.TryGetValue(databaseIdOrType, out string cachedConnectionString))
        {
            return cachedConnectionString;
        }

        // If it's the app database, use configuration
        if (databaseIdOrType == "AppDatabase")
        {
            string appConnectionString = _configuration.GetConnectionString("DefaultConnection");
            _connectionCache.TryAdd(databaseIdOrType, appConnectionString);
            return appConnectionString;
        }

        try
        {
            // Try to parse as database ID
            if (int.TryParse(databaseIdOrType, out int databaseId))
            {
                // Fetch by ID - implement using Dapper on _appDbConnection
                string query = "SELECT * FROM Databases WHERE DatabaseID = @DatabaseID AND IsActive = 1";
                var database = _appDbConnection.QueryFirstOrDefault<Database>(query, new { DatabaseID = databaseId });

                if (database == null)
                    throw new ArgumentException($"Database with ID {databaseId} not found or inactive");

                string connectionString = database.ConnectionString ?? BuildConnectionString(database);
                _connectionCache.TryAdd(databaseIdOrType, connectionString);
                return connectionString;
            }
            else
            {
                // Treat as database type name
                string query = "SELECT * FROM Databases d JOIN DatabaseTypes t ON d.TypeID = t.TypeID WHERE t.TypeName = @TypeName AND d.IsActive = 1";
                var database = _appDbConnection.QueryFirstOrDefault<Database>(query, new { TypeName = databaseIdOrType });

                if (database == null)
                    throw new ArgumentException($"No active database found for type {databaseIdOrType}");

                string connectionString = database.ConnectionString ?? BuildConnectionString(database);
                _connectionCache.TryAdd(databaseIdOrType, connectionString);
                return connectionString;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error retrieving connection string for {DatabaseIdOrType}", databaseIdOrType);
            throw;
        }
    }

    /// <summary>
    /// Gets the database type for a database by ID or type
    /// </summary>
    private string GetDatabaseType(string databaseIdOrType)
    {
        try
        {
            // If it's a type name, return directly
            if (!int.TryParse(databaseIdOrType, out int databaseId))
            {
                return databaseIdOrType;
            }

            // Query for database type
            string query = @"
                SELECT t.TypeName 
                FROM Databases d 
                JOIN DatabaseTypes t ON d.TypeID = t.TypeID 
                WHERE d.DatabaseID = @DatabaseID";

            return _appDbConnection.QueryFirstOrDefault<string>(query, new { DatabaseID = databaseId })
                ?? throw new ArgumentException($"Database type not found for ID {databaseId}");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error retrieving database type for {DatabaseIdOrType}", databaseIdOrType);
            throw;
        }
    }

    private string GetDatabaseTypeNameFromId(int typeId)
    {
        try
        {
            string query = "SELECT TypeName FROM DatabaseTypes WHERE TypeID = @TypeID";
            string typeName = _appDbConnection.QueryFirstOrDefault<string>(query, new { TypeID = typeId });

            if (string.IsNullOrEmpty(typeName))
            {
                // Fallback mapping if database query fails
                typeName = typeId switch
                {
                    1 => "SQLServer",
                    2 => "MySQL",
                    3 => "Oracle",
                    _ => $"Unknown_{typeId}"
                };

                _logger?.LogWarning("Using fallback type name {TypeName} for type ID {TypeID}", typeName, typeId);
            }

            return typeName;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error retrieving database type name for ID {TypeID}", typeId);

            // Fallback mapping if exception occurs
            return typeId switch
            {
                1 => "SQLServer",
                2 => "MySQL",
                3 => "Oracle",
                _ => $"Unknown_{typeId}"
            };
        }
    }

    // Helper methods for building connection strings
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
            // Implement your decryption logic here
            // For now, we'll just return the encrypted value as a placeholder
            return encryptedCredentials;

            // TODO: Implement proper decryption
            // Example: return _cryptoService.Decrypt(encryptedCredentials);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error decrypting credentials");
            return string.Empty;
        }
    }

    /// <summary>
    /// Clears the connection string cache
    /// </summary>
    public void ClearCache()
    {
        _connectionCache.Clear();
    }
}