using DynamicDashboardCommon.Models;


namespace DynamicDasboardWebAPI.Services
{
    public interface IDatabaseService
    {
        Task<IEnumerable<Database>> GetAllDatabasesAsync();
        Task<int> AddDatabaseAsync(Database database);
        bool GetDatabaseMetadataAsync(int databaseId);
    }
}
