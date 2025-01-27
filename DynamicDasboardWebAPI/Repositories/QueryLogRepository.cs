using System.Data;
using System.Threading.Tasks;
using Dapper;

namespace DynamicDasboardWebAPI.Repositories
{
    /// <summary>
    /// Repository for logging executed queries into the QueryLogs table.
    /// </summary>
    public class QueryLogsRepository(IDbConnection connection)
    {
        private readonly IDbConnection _connection = connection;

        /// <summary>
        /// Logs an executed query into the QueryLogs table.
        /// </summary>
        /// <param name="queryText">The SQL query executed.</param>
        /// <param name="executedBy">User ID of the executor (nullable).</param>
        /// <param name="databaseType">The type of database used.</param>
        /// <param name="result">Serialized result of the query (optional).</param>
        /// <returns>The number of rows affected.</returns>
        public async Task<int> LogQueryAsync(string queryText, int? executedBy, string databaseType, string result)
        {
            string sql = @"
                    INSERT INTO QueryLogs (QueryText, ExecutedAt, ExecutedBy, DatabaseType, Result)
                    VALUES (@QueryText, GETDATE(), @ExecutedBy, @DatabaseType, @Result)";

            return await _connection.ExecuteAsync(sql, new
            {
                QueryText = queryText,
                ExecutedBy = executedBy,
                DatabaseType = databaseType,
                Result = result
            });
        }
    }
}
