using DynamicDashboardCommon.Models;
using System.Data;
using DynamicDasboardWebAPI.Utilities;
using System.Data.Common;

public class DatabaseRepository
{
    private readonly IDbConnection _appDbConnection;
    private readonly DbConnectionFactory _connectionFactory;
    private readonly ILogger<DatabaseRepository> _logger;

    public DatabaseRepository(
        IDbConnection appDbConnection,
        DbConnectionFactory connectionFactory,
        ILogger<DatabaseRepository> logger = null)
    {
        _appDbConnection = appDbConnection ?? throw new ArgumentNullException(nameof(appDbConnection));
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _logger = logger;
    }

    // Get a database by ID
    public async Task<Database> GetDatabaseByIdAsync(int databaseId)
    {
        try
        {
            return await _appDbConnection.GetDatabaseByIdAsync(databaseId);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error retrieving database by ID: {DatabaseID}", databaseId);
            throw;
        }
    }

    public async Task<IEnumerable<Database>> GetAllDatabasesAsync()
    {
        try
        {
            return await _appDbConnection.GetAllDatabasesAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error retrieving all databases");
            throw;
        }
    }

    // Add a new database connection
    public async Task<int> AddDatabaseAsync(Database database)
    {
        try
        {
            const string query = @"
                INSERT INTO Databases 
                (Name, TypeID, ServerAddress, DatabaseName, Port, Username, EncryptedCredentials, 
                 ConnectionString, Description, CreatedBy, DBCreationScript, IsActive, CreatedAt) 
                VALUES 
                (@Name, @TypeID, @ServerAddress, @DatabaseName, @Port, @Username, @EncryptedCredentials, 
                 @ConnectionString, @Description, @CreatedBy, @DBCreationScript, @IsActive, @CreatedAt);
                SELECT CAST(SCOPE_IDENTITY() as int)";

            // Ensure necessary fields are set
            database.IsActive = database.IsActive ?? true;
            database.CreatedAt = DateTime.UtcNow;

            // Build connection string if not provided
            if (string.IsNullOrWhiteSpace(database.ConnectionString))
            {
                database.ConnectionString = _connectionFactory.BuildConnectionString(database);
            }

            return await _appDbConnection.ExecuteScalarSafeAsync<int>(query, database);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error adding database: {Name}", database?.DataBaseViewingName);
            throw;
        }
    }

    // Update an existing database connection
    public async Task<int> UpdateDatabaseAsync(Database database)
    {
        try
        {

            const string query = @"
                    UPDATE Databases 
                    SET Name = @Name, 
                        TypeID = @TypeID, 
                        ConnectionString = @ConnectionString,
                        Description = @Description,
                        DBCreationScript = @DBCreationScript,
                        IsActive = @IsActive,
                        ServerAddress = @ServerAddress, 
                        DatabaseName = @DatabaseName, 
                        Port = @Port, 
                        Username = @Username, 
                        EncryptedCredentials = @EncryptedCredentials
                    WHERE DatabaseID = @DatabaseID";

            return await _appDbConnection.ExecuteSafeAsync(query, database);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error updating database: {DatabaseID}", database?.DatabaseID);
            throw;
        }
    }

    // Soft delete a database connection by marking it as inactive
    public async Task<int> DeleteDatabaseAsync(int databaseId)
    {
        try
        {
            const string query = "UPDATE Databases SET IsActive = 0 WHERE DatabaseID = @DatabaseID";
            return await _appDbConnection.ExecuteSafeAsync(query, new { DatabaseID = databaseId });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error soft-deleting database: {DatabaseID}", databaseId);
            throw;
        }
    }

    // Additional CRUD methods for databases

    // Execute query on specific database
    public async Task<IEnumerable<T>> ExecuteQueryAsync<T>(int databaseId, string query, object parameters = null)
    {
        try
        {
            using var connection = await _connectionFactory.CreateOpenConnectionAsync(databaseId);
            return await connection.QuerySafeAsync<T>(query, parameters);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error executing query on database {DatabaseId}: {Query}", databaseId, query);
            throw;
        }
    }

    public async Task<bool> TestConnectionAsync(Database database)
    {
        try
        {
            // Test connection
            bool isSuccess = await _connectionFactory.TestConnectionAsync(database, database.ConnectionString);

            return isSuccess;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error testing connection with parameters: {Server}/{Database}",
                database.ServerAddress, database.DatabaseName);
            throw;
        }
    }

    public async Task<List<DatabaseType>> GetSupportedDatabaseTypesAsync()
    {
        try
        {
            _logger?.LogInformation("Retrieving supported database types");

            string query = "SELECT TypeID, TypeName FROM DatabaseTypes";
            // Get types from repository
            var databaseTypes = await _appDbConnection.QuerySafeAsync<DatabaseType>(query);
            return databaseTypes.ToList();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error retrieving supported database types");

            // Fallback only if database query fails
            throw;
        }
    }

    public async Task<string> GetDatabaseTypeNameAsync(int typeId)
    {
        try
        {
           string query = "SELECT TypeName FROM DatabaseTypes WHERE TypeID = @TypeID";

            var typeName = await _appDbConnection.QuerySingleOrDefaultSafeAsync<string>(query);


            return typeName;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error retrieving database type name for ID: {TypeID}", typeId);
            // Fallback
            throw;
        }
    }


}