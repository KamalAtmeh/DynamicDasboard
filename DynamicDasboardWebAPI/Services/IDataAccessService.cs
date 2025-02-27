using System.Collections.Generic;
using System.Threading.Tasks;

namespace DynamicDasboardWebAPI.Services
{
    /// <summary>
    /// Interface for generic database access operations
    /// </summary>
    public interface IDataAccessService
    {
        /// <summary>
        /// Executes a query and returns a list of results
        /// </summary>
        /// <typeparam name="T">Type to map results to</typeparam>
        /// <param name="dbType">Database type (SQLServer, MySQL, Oracle)</param>
        /// <param name="query">SQL query to execute</param>
        /// <param name="parameters">Optional parameters for the query</param>
        /// <returns>List of results</returns>
        Task<IEnumerable<T>> QueryAsync<T>(string dbType, string query, object parameters = null);

        /// <summary>
        /// Executes a query and returns a single result
        /// </summary>
        /// <typeparam name="T">Type to map the result to</typeparam>
        /// <param name="dbType">Database type (SQLServer, MySQL, Oracle)</param>
        /// <param name="query">SQL query to execute</param>
        /// <param name="parameters">Optional parameters for the query</param>
        /// <returns>Single result or default value</returns>
        Task<T> QueryFirstOrDefaultAsync<T>(string dbType, string query, object parameters = null);

        /// <summary>
        /// Executes a command and returns the number of affected rows
        /// </summary>
        /// <param name="dbType">Database type (SQLServer, MySQL, Oracle)</param>
        /// <param name="command">SQL command to execute</param>
        /// <param name="parameters">Optional parameters for the command</param>
        /// <returns>Number of affected rows</returns>
        Task<int> ExecuteAsync(string dbType, string command, object parameters = null);

        /// <summary>
        /// Executes a scalar query and returns a single value
        /// </summary>
        /// <typeparam name="T">Type of the scalar value</typeparam>
        /// <param name="dbType">Database type (SQLServer, MySQL, Oracle)</param>
        /// <param name="query">SQL query to execute</param>
        /// <param name="parameters">Optional parameters for the query</param>
        /// <returns>Scalar value</returns>
        Task<T> ExecuteScalarAsync<T>(string dbType, string query, object parameters = null);
    }
}