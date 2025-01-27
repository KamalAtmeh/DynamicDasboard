using Dapper;
using DynamicDashboardCommon.Models;
using DynamicDasboardWebAPI.Utilities;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace DynamicDasboardWebAPI.Repositories
{
    /// <summary>
    /// Repository class for managing database connections and operations.
    /// </summary>
    public class DatabaseRepository
    {
        private readonly IDbConnection _appDbConnection; // Application Database
        private readonly DbConnectionFactory _dynamicDbConnectionFactory; // Dynamic Database

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseRepository"/> class.
        /// </summary>
        /// <param name="appDbConnection">The application database connection.</param>
        /// <param name="dynamicDbConnectionFactory">The dynamic database connection factory.</param>
        public DatabaseRepository(IDbConnection appDbConnection, DbConnectionFactory dynamicDbConnectionFactory)
        {
            _appDbConnection = appDbConnection;
            _dynamicDbConnectionFactory = dynamicDbConnectionFactory;
        }

        /// <summary>
        /// Fetches all databases from the application database.
        /// </summary>
        /// <returns>A collection of <see cref="Database"/> objects.</returns>
        public async Task<IEnumerable<Database>> GetAllDatabasesAsync()
        {
            string query = "SELECT * FROM Databases";
            return await _appDbConnection.QueryAsync<Database>(query);
        }

        /// <summary>
        /// Adds a new database connection.
        /// </summary>
        /// <param name="database">The database entity to add.</param>
        /// <returns>The number of rows affected.</returns>
        public async Task<int> AddDatabaseAsync(Database database)
        {
            string query = "INSERT INTO Databases (Name, TypeID, ConnectionString, Description, CreatedBy, DBCreationScript) VALUES (@Name, @TypeID, @ConnectionString, @Description, @CreatedBy, @DBCreationScript)";
            var rowsaffected = await _appDbConnection.ExecuteAsync(query, database);
            return rowsaffected;
        }

        /// <summary>
        /// Updates an existing database connection.
        /// </summary>
        /// <param name="database">The database entity to update.</param>
        /// <returns>The number of rows affected.</returns>
        public async Task<int> UpdateDatabaseAsync(Database database)
        {
            string query = "UPDATE Databases SET Name = @Name, TypeID = @TypeID, ConnectionString = @ConnectionString WHERE DatabaseID = @DatabaseID";
            var rowsAffected = await _appDbConnection.ExecuteAsync(query, database);
            return rowsAffected;
        }

        /// <summary>
        /// Deletes a database connection.
        /// </summary>
        /// <param name="databaseId">The ID of the database to delete.</param>
        /// <returns>The number of rows affected.</returns>
        public async Task<int> DeleteDatabaseAsync(int databaseId)
        {
            string query = "DELETE FROM Databases WHERE DatabaseID = @DatabaseID";
            return await _appDbConnection.ExecuteAsync(query, new { DatabaseID = databaseId });
        }

        /// <summary>
        /// Tests the connection to a dynamic database.
        /// </summary>
        /// <param name="database">The database entity to test the connection for.</param>
        /// <returns><c>true</c> if the connection is successful; otherwise, <c>false</c>.</returns>
        public async Task<bool> TestConnectionAsync(Database database)
        {
            try
            {
                await _dynamicDbConnectionFactory.OpenConnectionAsync(GetDatabaseTypeName(database.TypeID));
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Helper method to convert TypeID to database type name.
        /// </summary>
        /// <param name="typeId">The type ID of the database.</param>
        /// <returns>The name of the database type.</returns>
        /// <exception cref="ArgumentException">Thrown when the type ID is invalid.</exception>
        private string GetDatabaseTypeName(int typeId)
        {
            return typeId switch
            {
                1 => "SQLServer",
                2 => "MySQL",
                3 => "Oracle",
                _ => throw new ArgumentException("Invalid database type.")
            };
        }
    }
}