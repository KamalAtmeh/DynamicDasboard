using Dapper;
using DynamicDashboardCommon.Models;
using DynamicDasboardWebAPI.Utilities;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using MySql.Data.MySqlClient;
using Oracle.ManagedDataAccess.Client;

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
        //public async Task<bool> TestConnectionAsync(Database database)
        //{
        //    try
        //    {
        //        await _dynamicDbConnectionFactory.OpenConnectionAsync(GetDatabaseTypeName(database.TypeID));
        //        return true;
        //    }
        //    catch
        //    {
        //        return false;
        //    }
        //}

        public async Task<bool> TestConnectionAsync(Database database)
        {
            try
            {
                string connectionString = database.ConnectionString;

                // Create connection based on database type
                using (IDbConnection connection = CreateConnection(database.TypeID, connectionString))
                {
                    // For proper async opening, we need to cast to the specific connection type
                    switch (database.TypeID)
                    {
                        case 1: // SQL Server
                            await ((SqlConnection)connection).OpenAsync();
                            break;
                        case 2: // MySQL
                            await ((MySqlConnection)connection).OpenAsync();
                            break;
                        case 3: // Oracle
                            await ((OracleConnection)connection).OpenAsync();
                            break;
                        default:
                            throw new NotSupportedException($"Database type ID '{database.TypeID}' is not supported.");
                    }

                    // Execute a simple query to verify connection
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = GetTestQuery(database.TypeID);
                        command.CommandType = CommandType.Text;

                        var result = command.ExecuteScalar();
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                return false;
            }
        }

        public async Task<Database> GetDatabaseByIdAsync(int databaseId)
        {
            string query = "SELECT * FROM Databases WHERE DatabaseID = @DatabaseID";

            // Using Dapper to execute the query
            return await _appDbConnection.QueryFirstOrDefaultAsync<Database>(query, new { DatabaseID = databaseId });
        }

        private IDbConnection CreateConnection(int typeId, string connectionString)
        {
            return typeId switch
            {
                1 => new SqlConnection(connectionString),
                2 => new MySqlConnection(connectionString),
                3 => new OracleConnection(connectionString),
                _ => throw new NotSupportedException($"Database type ID '{typeId}' is not supported.")
            };
        }

        private string GetTestQuery(int typeId)
        {
            return typeId switch
            {
                1 => "SELECT 1", // SQL Server
                2 => "SELECT 1", // MySQL
                3 => "SELECT 1 FROM DUAL", // Oracle
                _ => "SELECT 1"
            };
        }


    }
}