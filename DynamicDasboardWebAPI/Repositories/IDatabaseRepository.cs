using DynamicDashboardCommon.Models;

namespace DynamicDasboardWebAPI.Repositories
{
    /// <summary>
    /// Interface for database repository operations.
    /// Provides methods to interact with database entities and their metadata.
    /// </summary>
    public interface IDatabaseRepository
    {
        /// <summary>
        /// Retrieves all databases asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains a collection of databases.</returns>
        Task<IEnumerable<Database>> GetAllDatabasesAsync();

        /// <summary>
        /// Adds a new database asynchronously.
        /// </summary>
        /// <param name="database">The database entity to add.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the ID of the added database.</returns>
        Task<int> AddDatabaseAsync(Database database);

        /// <summary>
        /// Retrieves metadata for a specific database asynchronously.
        /// </summary>
        /// <param name="databaseId">The ID of the database.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a collection of tables associated with the database.</returns>
        Task<IEnumerable<Table>> GetDatabaseMetadataAsync(int databaseId);

        /// <summary>
        /// Retrieves a database by its name asynchronously.
        /// </summary>
        /// <param name="databaseName">The name of the database.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the database entity.</returns>
        Task<Database> GetDatabaseByNameAsync(string databaseName);
    }
}
