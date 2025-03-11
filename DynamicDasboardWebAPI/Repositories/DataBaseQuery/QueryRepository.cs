using DynamicDasboardWebAPI.Utilities;
using DynamicDashboardCommon.Models;
using System.Data;
using Dapper;

namespace DynamicDasboardWebAPI.Repositories
{
    public class QueryRepository : BaseRepository
    {
        public QueryRepository(
            IDbConnection appDbConnection,
            DbConnectionFactory connectionFactory)
            : base(appDbConnection, connectionFactory)
        {
        }

        /// <summary>
        /// Executes a SQL query on the default DB and returns the results as a list of dictionaries.
        /// </summary>
        /// <param name="query">The SQL query to execute.</param>
        public async Task<List<Dictionary<string, object>>> ExecuteQueryAsync(string query)
        {
            try
            {
                // Use BaseRepository's WithConnectionAsync for the default DB
                var data = await WithConnectionAsync(async conn =>
                {
                    var result = await conn.QuerySafeAsync<dynamic>(query);
                    return result;
                });

                return DatabaseHelper.ConvertToDictionaries(data);
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Executes a query on a specific (dynamic) database using its ID.
        /// </summary>
        public async Task<List<Dictionary<string, object>>> ExecuteQueryOnDatabaseAsync(string query, int databaseId)
        {
            try
            {
                if (!string.IsNullOrEmpty(query) && databaseId != 0)
                {
                    // Use WithConnectionAsync with databaseId for a dynamic DB connection
                    var data = await WithConnectionAsync(async conn =>
                    {
                        return await conn.ExecuteQueryAsDictionariesAsync(query);
                    }, databaseId);

                    return data;
                }
                else
                {
                    return new List<Dictionary<string, object>>();
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Retrieves metadata for a database (logic uses the default DB connection).
        /// </summary>
        public async Task<DatabaseMetadataDto> GetDatabaseMetadataAsync(int databaseId)
        {
            try
            {
                // The method has a 'databaseId' parameter for metadata logic, 
                // but it uses the default DB to fetch info from the 'Databases' table, etc.
                return await WithConnectionAsync(async conn =>
                {
                    return await DatabaseHelper.GetDatabaseMetadataAsync(conn, databaseId);
                });
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}