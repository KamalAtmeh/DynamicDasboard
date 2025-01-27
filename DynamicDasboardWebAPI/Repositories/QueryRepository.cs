using System.Data;
using System.Threading.Tasks;
using Dapper;

namespace DynamicDasboardWebAPI.Repositories
{
    /// <summary>
    /// Repository for executing dynamic SQL queries.
    /// </summary>
    public class QueryRepository
    {
        private readonly IDbConnection _connection;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryRepository"/> class.
        /// </summary>
        /// <param name="connection">The database connection to use.</param>
        public QueryRepository(IDbConnection connection)
        {
            _connection = connection;
        }

        /// <summary>
        /// Executes a dynamic SQL query and returns the result.
        /// </summary>
        /// <param name="query">The SQL query to execute.</param>
        /// <returns>The query result as a dynamic object.</returns>
        public async Task<dynamic> ExecuteQueryAsync(string query)
        {
            return await _connection.QueryAsync<dynamic>(query);
        }
    }
}
