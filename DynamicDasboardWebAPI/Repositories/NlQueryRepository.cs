using DynamicDasboardWebAPI.Utilities;
using DynamicDashboardCommon.Models;
using System.Data;

public class NlQueryRepository
{
    private readonly IDbConnection _appDbConnection;
    private readonly DbConnectionFactory _connectionFactory;
    private readonly ILogger<NlQueryRepository> _logger;

    public NlQueryRepository(
        IDbConnection appDbConnection,
        DbConnectionFactory connectionFactory,
        ILogger<NlQueryRepository> logger)
    {
        _appDbConnection = appDbConnection ?? throw new ArgumentNullException(nameof(appDbConnection));
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes a SQL query and returns the results as a list of dictionaries.
    /// </summary>
    /// <param name="query">The SQL query to execute.</param>
    /// <returns>A list of dictionaries, where each dictionary represents a row of data.</returns>
    public async Task<List<Dictionary<string, object>>> ExecuteQueryAsync(string query)
    {
        try
        {
            _logger.LogInformation("Executing query on application database");

            // Use the enhanced DatabaseHelper to execute and convert results
            var data = await _appDbConnection.QuerySafeAsync<dynamic>(query);
            return DatabaseHelper.ConvertToDictionaries(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing query on application database: {Query}", query);
            throw;
        }
    }

    /// <summary>
    /// Executes a query on a specific database using its ID
    /// </summary>
    /// <param name="query">SQL query to execute</param>
    /// <param name="databaseId">ID of the database in the system</param>
    /// <returns>Query results as a list of dictionaries</returns>
    public async Task<List<Dictionary<string, object>>> ExecuteQueryOnDatabaseAsync(string query, int databaseId)
    {
        try
        {
            _logger.LogInformation("Executing query on database ID {DatabaseId}: {Query}", databaseId, query);

            // Get database details
            var database = await _appDbConnection.GetDatabaseByIdAsync(databaseId);
            if (database == null)
            {
                throw new ArgumentException($"Database with ID {databaseId} not found");
            }

            // Create connection and execute query using the enhanced helper
            using var connection = await _connectionFactory.CreateOpenConnectionAsync(databaseId.ToString());
            return await connection.ExecuteQueryAsDictionariesAsync(query);
        }
        catch (DatabaseException ex)
        {
            _logger.LogError(ex, "Database error executing query on database ID {DatabaseId}: {Query}", databaseId, query);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing query on database ID {DatabaseId}: {Query}", databaseId, query);
            throw new DatabaseException($"Error executing query on database {databaseId}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Retrieves metadata for a database including tables and columns.
    /// </summary>
    /// <returns>A dictionary containing database metadata.</returns>
    public async Task<Dictionary<string, object>> GetDatabaseMetadataAsync(int databaseId)
    {
        try
        {
            _logger.LogInformation("Retrieving metadata for database ID {DatabaseId}", databaseId);

            var metadata = new Dictionary<string, object>();

            // Get tables
            var tables = await _appDbConnection.GetTablesByDatabaseIdAsync(databaseId);
            var tablesList = new List<object>();

            // For each table, get columns and relationships
            foreach (var table in tables)
            {
                var columns = await _appDbConnection.GetColumnsByTableIdAsync(table.TableID);
                var relationships = await _appDbConnection.GetRelationshipsByTableIdAsync(table.TableID);

                tablesList.Add(new
                {
                    table,
                    columns,
                    relationships
                });
            }

            metadata["tables"] = tablesList;
            return metadata;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving metadata for database ID {DatabaseId}", databaseId);
            throw new DatabaseException($"Error retrieving metadata for database {databaseId}: {ex.Message}", ex);
        }
    }
}