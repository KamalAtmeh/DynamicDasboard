using System;
using System.Data;
using System.Threading.Tasks;
using DynamicDashboardCommon.Models;
using Dapper;

namespace DynamicDasboardWebAPI.Repositories
{
    public class DatabaseJsonSchemaRepository
    {
        private readonly IDbConnection _dbConnection;

        public DatabaseJsonSchemaRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<int> InsertDatabaseJsonSchemaAsync(DatabaseJsonSchema schema)
        {
            var sql = @"
INSERT INTO DatabaseSchemas (Name, Status, SchemaData, CreatedAt, ModifiedAt)
VALUES (@Name, @Status, @SchemaData, GETUTCDATE(), GETUTCDATE());
SELECT CAST(SCOPE_IDENTITY() as int);";

            return await _dbConnection.ExecuteScalarAsync<int>(sql, schema);
        }

        public async Task<int> UpdateDatabaseJsonSchemaAsync(DatabaseJsonSchema schema)
        {
            var sql = @"
UPDATE DatabaseSchemas
SET Name = @Name,
    Status = @Status,
    SchemaData = @SchemaData,
    ModifiedAt = GETUTCDATE()
WHERE Id = @Id;";

            return await _dbConnection.ExecuteAsync(sql, schema);
        }

        public async Task<DatabaseJsonSchema> GetDatabaseJsonSchemaByIdAsync(int id)
        {
            var sql = "SELECT * FROM DatabaseSchemas WHERE Id = @Id;";
            return await _dbConnection.QueryFirstOrDefaultAsync<DatabaseJsonSchema>(sql, new { Id = id });
        }

        public async Task<int> DeactivateDatabaseJsonSchemaAsync(int id)
        {
            // Set Status = 0 to indicate deactivation.
            var sql = @"
UPDATE DatabaseSchemas
SET Status = 0, ModifiedAt = GETUTCDATE()
WHERE Id = @Id;";

            return await _dbConnection.ExecuteAsync(sql, new { Id = id });
        }
    }
}
