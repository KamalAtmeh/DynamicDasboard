using Dapper;
using DynamicDashboardCommon.Models;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace DynamicDasboardWebAPI.Repositories
{
    /// <summary>
    /// Repository class for managing table entities in the dynamic dashboard system.
    /// This class provides methods to perform CRUD operations on the Tables table in the database.
    /// </summary>
    public class TableRepository
    {
        private readonly IDbConnection _connection;

        /// <summary>
        /// Initializes a new instance of the <see cref="TableRepository"/> class.
        /// </summary>
        /// <param name="connection">The database connection to be used by the repository.</param>
        public TableRepository(IDbConnection connection)
        {
            _connection = connection;
        }

        /// <summary>
        /// Fetches all tables for a specific database.
        /// </summary>
        /// <param name="databaseId">The ID of the database.</param>
        /// <returns>A collection of tables associated with the specified database.</returns>
        public async Task<IEnumerable<Table>> GetTablesByDatabaseIdAsync(int databaseId)
        {
            string query = "SELECT * FROM Tables WHERE DatabaseID = @DatabaseID";
            return await _connection.QueryAsync<Table>(query, new { DatabaseID = databaseId });
        }

        /// <summary>
        /// Adds a new table to the database.
        /// </summary>
        /// <param name="table">The table entity to be added.</param>
        /// <returns>The number of rows affected.</returns>
        public async Task<int> AddTableAsync(Table table)
        {
            string query = @"
                    INSERT INTO Tables (DatabaseID, DBTableName, AdminTableName, AdminDescription)
                    VALUES (@DatabaseID, @DBTableName, @AdminTableName, @AdminDescription)";
            return await _connection.ExecuteAsync(query, table);
        }

        /// <summary>
        /// Updates an existing table in the database.
        /// </summary>
        /// <param name="table">The table entity with updated values.</param>
        /// <returns>The number of rows affected.</returns>
        public async Task<int> UpdateTableAsync(Table table)
        {
            string query = @"
                    UPDATE Tables
                    SET DBTableName = @DBTableName, AdminTableName = @AdminTableName, AdminDescription = @AdminDescription
                    WHERE TableID = @TableID";
            return await _connection.ExecuteAsync(query, table);
        }

        /// <summary>
        /// Deletes a table from the database.
        /// </summary>
        /// <param name="tableId">The ID of the table to be deleted.</param>
        /// <returns>The number of rows affected.</returns>
        public async Task<int> DeleteTableAsync(int tableId)
        {
            string query = "DELETE FROM Tables WHERE TableID = @TableID";
            return await _connection.ExecuteAsync(query, new { TableID = tableId });
        }
    }
}