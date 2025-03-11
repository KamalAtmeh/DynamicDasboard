using DynamicDashboardCommon.Models;
using System.Data;
using DynamicDasboardWebAPI.Utilities;
using System.Data.Common;
using Dapper;

namespace DynamicDasboardWebAPI.Repositories
{
    public class DatabaseRepository : BaseRepository
    {
        #region CONSTRUCTOR

        public DatabaseRepository(
            IDbConnection appDbConnection,
            DbConnectionFactory connectionFactory)
            : base(appDbConnection, connectionFactory)
        {
        }

        #endregion

        #region PUBLIC_METHODS_DEFAULT_DB

        /// <summary>
        /// Get a database by ID (uses default DB).
        /// </summary>
        public async Task<Database> GetDatabaseByIdAsync(int databaseId)
        {
            try
            {
                return await WithConnectionAsync(async conn =>
                {
                    // Extension method from DatabaseHelper: GetDatabaseByIdAsync
                    return await conn.GetDatabaseByIdAsync(databaseId);
                });
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Get all databases (uses default DB).
        /// </summary>
        public async Task<IEnumerable<Database>> GetAllDatabasesAsync()
        {
            try
            {
                return await WithConnectionAsync(async conn =>
                {
                    return await conn.GetAllDatabasesAsync();
                });
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Add a new database connection (uses default DB).
        /// </summary>
        public async Task<int> AddDatabaseAsync(Database database)
        {
            try
            {
                const string query = @"
                   
                    INSERT INTO [dbo].[Databases]
                    ([Name], TypeID, ServerAddress, [FriendlyName], Port, Username, EncryptedCredentials, 
                     ConnectionString, Description, CreatedBy, DBCreationScript, IsActive, CreatedAt) 
                    VALUES 
                    (@Name, @TypeID, @ServerAddress, @FriendlyName, @Port, @Username, @EncryptedCredentials, 
                     @ConnectionString, @Description, @CreatedBy, @DBCreationScript, @IsActive, @CreatedAt);
                    SELECT CAST(SCOPE_IDENTITY() as int)";

                // Set fields if needed
                database.IsActive = database.IsActive;
                database.CreatedAt = DateTime.UtcNow;

                // Build connection string if not provided
                if (string.IsNullOrWhiteSpace(database.ConnectionString))
                {
                    database.ConnectionString = _connectionFactory.BuildConnectionString(database);
                }

                return await WithConnectionAsync(async conn =>
                {
                    return await conn.ExecuteScalarSafeAsync<int>(query, database);
                });
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Update an existing database connection (uses default DB).
        /// </summary>
        public async Task<int> UpdateDatabaseAsync(Database database)
        {
            try
            {
                const string query = @"
                    UPDATE Databases 
                    SET [Name] = @Name,
                        [FriendlyName] = @FriendlyName, 
                        TypeID = @TypeID, 
                        ConnectionString = @ConnectionString,
                        Description = @Description,
                        DBCreationScript = @DBCreationScript,
                        IsActive = @IsActive,
                        ServerAddress = @ServerAddress, 
                        Port = @Port, 
                        Username = @Username, 
                        EncryptedCredentials = @EncryptedCredentials
                    WHERE DatabaseID = @DatabaseID";

                return await WithConnectionAsync(async conn =>
                {
                    return await conn.ExecuteSafeAsync(query, database);
                });
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Soft-delete a database connection by marking it inactive (uses default DB).
        /// </summary>
        public async Task<int> DeleteDatabaseAsync(int databaseId)
        {
            try
            {
                const string query = "UPDATE Databases SET IsActive = 0 WHERE DatabaseID = @DatabaseID";
                return await WithConnectionAsync(async conn =>
                {
                    return await conn.ExecuteSafeAsync(query, new { DatabaseID = databaseId });
                });
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Get supported database types (uses default DB).
        /// </summary>
        public async Task<List<DatabaseType>> GetSupportedDatabaseTypesAsync()
        {
            try
            {
                const string query = "SELECT TypeID, TypeName FROM DatabaseTypes";

                var databaseTypes = await WithConnectionAsync(async conn =>
                {
                    return await conn.QuerySafeAsync<DatabaseType>(query);
                });

                return databaseTypes.ToList();
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Get a database type name by its ID (uses default DB).
        /// </summary>
        public async Task<string> GetDatabaseTypeNameAsync(int typeId)
        {
            try
            {
                const string query = "SELECT TypeName FROM DatabaseTypes WHERE TypeID = @TypeID";

                return await WithConnectionAsync(async conn =>
                {
                    return await conn.QuerySingleOrDefaultSafeAsync<string>(query, new { TypeID = typeId });
                });
            }
            catch (Exception)
            {
                throw;
            }
        }

        #endregion

        #region PUBLIC_METHODS_CONNECTION_FACTORY

        /// <summary>
        /// Test a database connection using DbConnectionFactory logic (no direct DB usage).
        /// </summary>
        public async Task<bool> TestConnectionAsync(Database database)
        {
            try
            {
                // We simply call the factory method:
                bool isSuccess = await _connectionFactory.TestConnectionAsync(database, database.ConnectionString);
                return isSuccess;
            }
            catch (Exception)
            {
                throw;
            }
        }

        #endregion
    }
}
