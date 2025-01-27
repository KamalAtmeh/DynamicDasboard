using DynamicDashboardCommon.Models;


namespace DynamicDasboardWebAPI.Services
{
    public interface IDatabaseService
    {
        /// <summary>
        /// Asynchronously retrieves all databases.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains an enumerable collection of Database objects.</returns>
        Task<IEnumerable<Database>> GetAllDatabasesAsync();

        /// <summary>
        /// Asynchronously adds a new database.
        /// </summary>
        /// <param name="database">The database object to add.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the ID of the newly added database.</returns>
        Task<int> AddDatabaseAsync(Database database);

        /// <summary>
        /// Asynchronously retrieves metadata for a specific database.
        /// </summary>
        /// <param name="databaseId">The ID of the database to retrieve metadata for.</param>
        /// <returns>A boolean indicating whether the metadata retrieval was successful.</returns>
        bool GetDatabaseMetadataAsync(int databaseId);
    }
}
