using DynamicDashboardCommon.Models;

namespace DynamicDasboardWebAPI.Repositories
{
    public interface IDatabaseRepository
    {
        Task<IEnumerable<Database>> GetAllDatabasesAsync();
        Task<int> AddDatabaseAsync(Database database);
        Task<IEnumerable<Table>> GetDatabaseMetadataAsync(int databaseId);
        Task<Database> GetDatabaseByNameAsync(string databaseName);
    }
}
