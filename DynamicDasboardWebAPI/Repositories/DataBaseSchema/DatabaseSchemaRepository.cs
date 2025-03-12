using System;
using System.Data;
using System.Threading.Tasks;
using DynamicDashboardCommon.Models;
using Dapper;
using DynamicDasboardWebAPI.Utilities;
using DynamicDashboardCommon.Enums;

namespace DynamicDasboardWebAPI.Repositories
{
    public class DatabaseSchemaRepository : BaseRepository
    {
        private readonly DatabaseRepository _DataBaseRepository;

        public DatabaseSchemaRepository(
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
INSERT INTO DatabaseSchemas (Name, Status, SchemaData, CreatedAt, ModifiedAt, DataBaseID)
VALUES (@Name, @Status, @SchemaData, GETUTCDATE(), GETUTCDATE(), @DataBaseID);
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
WHERE DataBaseID = @Id;";

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
                var sql = "SELECT * FROM DatabaseSchemas WHERE DataBaseID = @Id;";

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
WHERE DataBaseID = @Id;";

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

        /// <summary>
        /// Retrieves tables from a connected database.
        /// </summary>
        public async Task<IEnumerable<dynamic>> GetTablesAsync(Database objDataBase)
        {
            //var db = await _DataBaseRepository.GetDatabaseByIdAsync(databaseId);
            try
            {
                return await WithConnectionAsync(async conn =>
                {
                    string sql;

                    switch (objDataBase.TypeID)
                    {
                        case (int)EnumDatabaseType.SQLServer:
                            sql = @"
                            SELECT 
                                TABLE_CATALOG,
                                TABLE_SCHEMA,
                                TABLE_NAME, 
                                TABLE_TYPE
                            FROM 
                                INFORMATION_SCHEMA.TABLES
                            WHERE 
                                TABLE_TYPE = 'BASE TABLE'
                                AND TABLE_NAME NOT LIKE 'sys%'
                            ORDER BY 
                                TABLE_NAME";
                            break;

                        case (int)EnumDatabaseType.MySQL:
                            sql = @"
                            SELECT 
                                TABLE_CATALOG,
                                TABLE_SCHEMA,
                                TABLE_NAME, 
                                TABLE_TYPE
                            FROM 
                                INFORMATION_SCHEMA.TABLES
                            WHERE 
                                TABLE_SCHEMA = DATABASE()
                                AND TABLE_TYPE = 'BASE TABLE'
                            ORDER BY 
                                TABLE_NAME";
                            break;

                        case (int)EnumDatabaseType.Oracle:
                            sql = @"
                            SELECT 
                                NULL AS TABLE_CATALOG,
                                OWNER AS TABLE_SCHEMA,
                                TABLE_NAME, 
                                'BASE TABLE' AS TABLE_TYPE
                            FROM 
                                ALL_TABLES
                            WHERE 
                                OWNER = USER
                            ORDER BY 
                                TABLE_NAME";
                            break;

                        default:
                            throw new NotSupportedException($"Database type {objDataBase.TypeID} not supported");
                    }

                    return await conn.QueryAsync(sql);
                }, objDataBase.DatabaseID);
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Retrieves columns for a specific table.
        /// </summary>
        public async Task<IEnumerable<dynamic>> GetColumnsAsync(Database objDataBase, string tableName)
        {
            try
            {
                return await WithConnectionAsync(async conn =>
                {

                    string sql;
                    object parameters;

                    switch (objDataBase.TypeID)
                    {
                        case (int)EnumDatabaseType.SQLServer:
                            sql = @"
                            SELECT 
                                c.COLUMN_NAME,
                                c.DATA_TYPE,
                                c.IS_NULLABLE,
                                c.ORDINAL_POSITION,
                                CASE 
                                    WHEN pk.COLUMN_NAME IS NOT NULL THEN 1
                                    ELSE 0
                                END AS IsPrimaryKey
                            FROM 
                                INFORMATION_SCHEMA.COLUMNS c
                            LEFT JOIN (
                                SELECT 
                                    k.TABLE_SCHEMA,
                                    k.TABLE_NAME,
                                    k.COLUMN_NAME
                                FROM 
                                    INFORMATION_SCHEMA.KEY_COLUMN_USAGE k
                                INNER JOIN 
                                    INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                                    ON k.CONSTRAINT_NAME = tc.CONSTRAINT_NAME
                                WHERE 
                                    tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
                            ) pk
                            ON  c.TABLE_SCHEMA = pk.TABLE_SCHEMA
                                AND c.TABLE_NAME = pk.TABLE_NAME
                                AND c.COLUMN_NAME = pk.COLUMN_NAME
                            WHERE 
                                c.TABLE_NAME = @TableName
                            ORDER BY 
                                c.ORDINAL_POSITION";
                            parameters = new { TableName = tableName };
                            break;

                        case (int)EnumDatabaseType.MySQL:
                            sql = @"
                            SELECT 
                                c.COLUMN_NAME,
                                c.DATA_TYPE,
                                c.IS_NULLABLE,
                                c.ORDINAL_POSITION,
                                c.COLUMN_KEY = 'PRI' AS IsPrimaryKey
                            FROM 
                                INFORMATION_SCHEMA.COLUMNS c
                            WHERE 
                                c.TABLE_SCHEMA = DATABASE()
                                AND c.TABLE_NAME = @TableName
                            ORDER BY 
                                c.ORDINAL_POSITION";
                            parameters = new { TableName = tableName };
                            break;

                        case (int)EnumDatabaseType.Oracle:
                            sql = @"
                            SELECT 
                                c.COLUMN_NAME,
                                c.DATA_TYPE,
                                CASE WHEN c.NULLABLE = 'Y' THEN 'YES' ELSE 'NO' END AS IS_NULLABLE,
                                c.COLUMN_ID AS ORDINAL_POSITION,
                                CASE WHEN p.COLUMN_NAME IS NOT NULL THEN 1 ELSE 0 END AS IsPrimaryKey
                            FROM 
                                ALL_TAB_COLUMNS c
                            LEFT JOIN (
                                SELECT
                                    acc.COLUMN_NAME
                                FROM
                                    ALL_CONS_COLUMNS acc
                                JOIN
                                    ALL_CONSTRAINTS ac
                                    ON acc.CONSTRAINT_NAME = ac.CONSTRAINT_NAME
                                WHERE
                                    ac.CONSTRAINT_TYPE = 'P'
                                    AND ac.TABLE_NAME = :TableName
                            ) p ON c.COLUMN_NAME = p.COLUMN_NAME
                            WHERE 
                                c.TABLE_NAME = :TableName
                                AND c.OWNER = USER
                            ORDER BY 
                                c.COLUMN_ID";
                            parameters = new { TableName = tableName };
                            break;

                        default:
                            throw new Exception($"Database type {objDataBase.TypeID} not supported");
                    }

                    return await conn.QueryAsync(sql, parameters);
                }, objDataBase.DatabaseID);
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Retrieves relationships from a connected database.
        /// </summary>
        public async Task<IEnumerable<dynamic>> GetRelationshipsAsync(Database objDataBase)
        {
            try
            {
                return await WithConnectionAsync(async conn =>
                {

                    string sql;

                    switch (objDataBase.TypeID)
                    {
                        case (int)EnumDatabaseType.SQLServer:
                            sql = @"
                            SELECT
                                fk.name AS FK_NAME,
                                OBJECT_NAME(fk.parent_object_id) AS FK_TABLE,
                                COL_NAME(fkc.parent_object_id, fkc.parent_column_id) AS FK_COLUMN,
                                OBJECT_NAME(fk.referenced_object_id) AS PK_TABLE,
                                COL_NAME(fkc.referenced_object_id, fkc.referenced_column_id) AS PK_COLUMN
                            FROM 
                                sys.foreign_keys fk
                            INNER JOIN 
                                sys.foreign_key_columns fkc
                                ON fk.OBJECT_ID = fkc.constraint_object_id
                            ORDER BY 
                                FK_TABLE, FK_NAME, FK_COLUMN";
                            break;

                        case (int)EnumDatabaseType.MySQL:
                            sql = @"
                            SELECT
                                kcu.CONSTRAINT_NAME AS FK_NAME,
                                kcu.TABLE_NAME AS FK_TABLE,
                                kcu.COLUMN_NAME AS FK_COLUMN,
                                kcu.REFERENCED_TABLE_NAME AS PK_TABLE,
                                kcu.REFERENCED_COLUMN_NAME AS PK_COLUMN
                            FROM
                                INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu
                            WHERE
                                kcu.REFERENCED_TABLE_SCHEMA = DATABASE()
                                AND kcu.REFERENCED_TABLE_NAME IS NOT NULL
                            ORDER BY
                                kcu.TABLE_NAME, kcu.CONSTRAINT_NAME, kcu.ORDINAL_POSITION";
                            break;

                        case (int)EnumDatabaseType.Oracle:
                            sql = @"
                            SELECT
                                a.CONSTRAINT_NAME AS FK_NAME,
                                a.TABLE_NAME AS FK_TABLE,
                                a.COLUMN_NAME AS FK_COLUMN,
                                c_pk.TABLE_NAME AS PK_TABLE,
                                c_pk.COLUMN_NAME AS PK_COLUMN
                            FROM
                                ALL_CONS_COLUMNS a
                            JOIN
                                ALL_CONSTRAINTS c ON a.OWNER = c.OWNER AND a.CONSTRAINT_NAME = c.CONSTRAINT_NAME
                            JOIN
                                ALL_CONSTRAINTS c_pk ON c.R_OWNER = c_pk.OWNER AND c.R_CONSTRAINT_NAME = c_pk.CONSTRAINT_NAME
                            JOIN
                                ALL_CONS_COLUMNS c_pk_cols ON c_pk.OWNER = c_pk_cols.OWNER AND c_pk.CONSTRAINT_NAME = c_pk_cols.CONSTRAINT_NAME
                                AND c_pk_cols.POSITION = a.POSITION
                            WHERE
                                c.CONSTRAINT_TYPE = 'R'
                                AND a.OWNER = USER
                            ORDER BY
                                a.TABLE_NAME, a.CONSTRAINT_NAME, a.POSITION";
                            break;

                        default:
                            throw new NotSupportedException($"Database type {objDataBase.TypeID} not supported");
                    }

                    return await conn.QueryAsync(sql);
                }, objDataBase.DatabaseID);
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}


