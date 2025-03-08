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
    public async Task<List<Dictionary<string, object>>> ExecuteQueryOnDatabaseAsync(string query, int databaseId)
    { 

        try
        {
            if (!string.IsNullOrEmpty(query) && databaseId != 0)
            {
                _logger.LogInformation("Executing query on database ID {DatabaseId}: {Query}", databaseId, query);

                // Create connection and execute query using the enhanced helper
                using var connection = await _connectionFactory.CreateOpenConnectionAsync(databaseId);

                return await connection.ExecuteQueryAsDictionariesAsync(query);
            }
            else
            {
                return new List<Dictionary<string, object>>();
            }
        }

        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing query on database ID {DatabaseId}: {Query}", databaseId, query);
            throw;
        }
    }

    /// <summary>
    /// Retrieves metadata for a database including tables and columns.
    /// </summary>
    /// <returns>A dictionary containing database metadata.</returns>
    public async Task<DatabaseMetadataDto> GetDatabaseMetadataAsync(int databaseId)
    {
        try
        {
            _logger.LogInformation("Retrieving metadata for database ID {DatabaseId}", databaseId);

            return await _connectionFactory.ExecuteWithAppConnectionAsync(async conn =>
            {
                return await DatabaseHelper.GetCompleteDatabaseMetadataAsync(conn, databaseId, _logger);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving metadata for database ID {DatabaseId}", databaseId);
            throw;
        }
    }

    // DTO to represent database metadata
    public class DatabaseMetadataDto
    {
        public int DatabaseId { get; set; }
        public List<TableMetadataDto> Tables { get; set; }
    }

    public class TableMetadataDto
    {
        public Table Table { get; set; }
        public IEnumerable<Column> Columns { get; set; }
        public IEnumerable<Relationship> Relationships { get; set; }
    }
}