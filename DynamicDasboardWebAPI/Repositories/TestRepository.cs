using System.Data;
using System.Threading.Tasks;
using Dapper;

namespace DynamicDasboardWebAPI.Repositories
{
    /// <summary>
    /// Repository for testing database connectivity.
    /// </summary>
    public class TestRepository
    {
        private readonly IDbConnection _connection;

        public TestRepository(IDbConnection connection)
        {
            _connection = connection;
        }

        /// <summary>
        /// Tests the database connection by executing a simple query.
        /// </summary>
        /// <returns>True if the connection works; otherwise, false.</returns>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                var result = await _connection.QueryFirstOrDefaultAsync<int>("SELECT 1");
                return result == 1;
            }
            catch
            {
                return false;
            }
        }
    }
}
