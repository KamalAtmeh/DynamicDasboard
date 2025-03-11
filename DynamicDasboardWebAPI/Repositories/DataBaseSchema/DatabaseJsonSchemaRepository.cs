using System;
using System.Data;
using System.Threading.Tasks;
using DynamicDashboardCommon.Models;
using Dapper;
using DynamicDasboardWebAPI.Utilities;

namespace DynamicDasboardWebAPI.Repositories
{
    public class DatabaseJsonSchemaRepository : BaseRepository
    {
        public DatabaseJsonSchemaRepository(
            IDbConnection dbConnection,
            DbConnectionFactory connectionFactory)
            : base(dbConnection, connectionFactory)
        {
        }

        public async Task<int> InsertDatabaseJsonSchemaAsync(DatabaseSchema schema)
        {
            try
            {
                var sql = @"
INSERT INTO DatabaseSchemas (Name, Status, SchemaData, CreatedAt, ModifiedAt)
VALUES (@Name, @Status, @SchemaData, GETUTCDATE(), GETUTCDATE());
SELECT CAST(SCOPE_IDENTITY() as int);";

                return await WithConnectionAsync(async conn =>
                {
                    return await conn.ExecuteScalarAsync<int>(sql, schema);
                });
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<int> UpdateDatabaseJsonSchemaAsync(DatabaseSchema schema)
        {
            try
            {
                var sql = @"
UPDATE DatabaseSchemas
SET Name = @Name,
    Status = @Status,
    SchemaData = @SchemaData,
    ModifiedAt = GETUTCDATE()
WHERE Id = @Id;";

                return await WithConnectionAsync(async conn =>
                {
                    return await conn.ExecuteAsync(sql, schema);
                });
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<DatabaseSchema> GetDatabaseJsonSchemaByIdAsync(int id)
        {
            try
            {
                var sql = "SELECT * FROM DatabaseSchemas WHERE Id = @Id;";

                return await WithConnectionAsync(async conn =>
                {
                    return await conn.QueryFirstOrDefaultAsync<DatabaseSchema>(sql, new { Id = id });
                });
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<int> DeactivateDatabaseJsonSchemaAsync(int id)
        {
            try
            {
                // Set Status = 0 to indicate deactivation.
                var sql = @"
UPDATE DatabaseSchemas
SET Status = 0, ModifiedAt = GETUTCDATE()
WHERE Id = @Id;";

                return await WithConnectionAsync(async conn =>
                {
                    return await conn.ExecuteAsync(sql, new { Id = id });
                });
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
