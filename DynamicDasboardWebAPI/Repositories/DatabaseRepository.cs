using Dapper;
using DynamicDashboardCommon.Models;
using DynamicDasboardWebAPI.Utilities;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace DynamicDasboardWebAPI.Repositories
{
    public class DatabaseRepository
    {
        private readonly IDbConnection _appDbConnection; // Application Database
        private readonly DbConnectionFactory _dynamicDbConnectionFactory; // Dynamic Database

        public DatabaseRepository(IDbConnection appDbConnection, DbConnectionFactory dynamicDbConnectionFactory)
        {
            _appDbConnection = appDbConnection;
            _dynamicDbConnectionFactory = dynamicDbConnectionFactory;
        }

        // Fetch all databases from the Application Database
        public async Task<IEnumerable<Database>> GetAllDatabasesAsync()
        {
            string query = "SELECT * FROM Databases";
            return await _appDbConnection.QueryAsync<Database>(query);
        }

        // Add a new database connection
        public async Task<int> AddDatabaseAsync(Database database)
        {
            string query = "INSERT INTO Databases (Name, TypeID, ConnectionString, Description, CreatedBy, DBCreationScript) VALUES (@Name, @TypeID, @ConnectionString, @Description, @CreatedBy, @DBCreationScript)";
            var rowsaffected =  await _appDbConnection.ExecuteAsync(query, database);
            return rowsaffected;
        }

        // Update an existing database connection
        public async Task<int> UpdateDatabaseAsync(Database database)
        {
            string query = "UPDATE Databases SET Name = @Name, TypeID = @TypeID, ConnectionString = @ConnectionString WHERE DatabaseID = @DatabaseID";
            var rowsAffected = await _appDbConnection.ExecuteAsync(query, database);
            return rowsAffected;
        }

        // Delete a database connection
        public async Task<int> DeleteDatabaseAsync(int databaseId)
        {
            string query = "DELETE FROM Databases WHERE DatabaseID = @DatabaseID";
            return await _appDbConnection.ExecuteAsync(query, new { DatabaseID = databaseId });
        }

        // Test the connection to a dynamic database
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

        // Helper method to convert TypeID to database type name
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