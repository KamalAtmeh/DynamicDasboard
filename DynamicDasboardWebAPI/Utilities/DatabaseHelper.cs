using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using DynamicDashboardCommon.Models;

namespace DynamicDasboardWebAPI.Utilities
{
    /// <summary>
    /// Helper class for standardized database operations using Dapper and ADO.NET
    /// Provides extension methods for IDbConnection and other database utilities
    /// </summary>
    public static class DatabaseHelper
    {



        #region Safe Query Execution Methods

        /// <summary>
        /// Executes a query safely and maps the result to a list of entities
        /// </summary>
        public static async Task<IEnumerable<T>> QuerySafeAsync<T>(this IDbConnection connection, string sql, object param = null,
            IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            try
            {
                return await connection.QueryAsync<T>(sql, param, transaction, commandTimeout, commandType);
            }
            catch (Exception ex)
            {
                throw new DatabaseException("Error executing query", ex);
            }
        }

        /// <summary>
        /// Executes a command safely and returns the number of affected rows
        /// </summary>
        public static async Task<int> ExecuteSafeAsync(this IDbConnection connection, string sql, object param = null,
            IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            try
            {
                return await connection.ExecuteAsync(sql, param, transaction, commandTimeout, commandType);
            }
            catch (Exception ex)
            {
                throw new DatabaseException("Error executing command", ex);
            }
        }

        /// <summary>
        /// Executes a query safely and returns the first result or default value
        /// </summary>
        public static async Task<T> QueryFirstOrDefaultSafeAsync<T>(this IDbConnection connection, string sql, object param = null,
            IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            try
            {
                return await connection.QueryFirstOrDefaultAsync<T>(sql, param, transaction, commandTimeout, commandType);
            }
            catch (Exception ex)
            {
                throw new DatabaseException("Error executing query", ex);
            }
        }

        /// <summary>
        /// Executes a query safely and returns a single result
        /// </summary>
        public static async Task<T> QuerySingleOrDefaultSafeAsync<T>(this IDbConnection connection, string sql, object param = null,
            IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            try
            {
                return await connection.QuerySingleOrDefaultAsync<T>(sql, param, transaction, commandTimeout, commandType);
            }
            catch (Exception ex)
            {
                throw new DatabaseException("Error executing query", ex);
            }
        }

        /// <summary>
        /// Executes a scalar query safely and returns the result
        /// </summary>
        public static async Task<T> ExecuteScalarSafeAsync<T>(this IDbConnection connection, string sql, object param = null,
            IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            try
            {
                return await connection.ExecuteScalarAsync<T>(sql, param, transaction, commandTimeout, commandType);
            }
            catch (Exception ex)
            {
                throw new DatabaseException("Error executing scalar query", ex);
            }
        }

        #endregion

        #region Dictionary Result Methods

        /// <summary>
        /// Executes a query and returns the results as a list of dictionaries
        /// </summary>
        public static async Task<List<Dictionary<string, object>>> ExecuteQueryAsDictionariesAsync(this IDbConnection connection,
            string sql, object parameters = null, IDbTransaction transaction = null, int commandTimeout = 30)
        {
            try
            {
                var result = new List<Dictionary<string, object>>();

                using var command = connection.CreateCommand();
                command.CommandText = sql;
                command.CommandTimeout = commandTimeout;

                if (transaction != null)
                    command.Transaction = transaction;

                if (parameters != null)
                {
                    // Add parameters to command
                    if (command is DbCommand dbCommand)
                    {
                        foreach (var prop in parameters.GetType().GetProperties())
                        {
                            var param = dbCommand.CreateParameter();
                            param.ParameterName = prop.Name;
                            param.Value = prop.GetValue(parameters) ?? DBNull.Value;
                            dbCommand.Parameters.Add(param);
                        }
                    }
                }

                // Use different approaches based on connection type
                if (connection is DbConnection dbConnection && command is DbCommand cmd)
                {
                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        var row = new Dictionary<string, object>();
                        for (var i = 0; i < reader.FieldCount; i++)
                        {
                            var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                            row[reader.GetName(i)] = value;
                        }
                        result.Add(row);
                    }
                }
                else
                {
                    using var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        var row = new Dictionary<string, object>();
                        for (var i = 0; i < reader.FieldCount; i++)
                        {
                            var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                            row[reader.GetName(i)] = value;
                        }
                        result.Add(row);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new DatabaseException($"Error executing query as dictionaries: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Converts dynamic query results to a list of dictionaries
        /// </summary>
        public static List<Dictionary<string, object>> ConvertToDictionaries(IEnumerable<dynamic> results)
        {
            var dictResults = new List<Dictionary<string, object>>();

            foreach (var item in results)
            {
                var dict = new Dictionary<string, object>();
                foreach (var prop in (IDictionary<string, object>)item)
                {
                    dict[prop.Key] = prop.Value;
                }
                dictResults.Add(dict);
            }

            return dictResults;
        }

        #endregion

        #region Dynamic Database Helpers

        /// <summary>
        /// Gets a database ID by its name from the application database
        /// </summary>
        public static async Task<int> GetDatabaseIdByNameAsync(this IDbConnection connection, string databaseName)
        {
            try
            {
                const string sql = "SELECT DatabaseID FROM Databases WHERE Name = @Name AND IsActive = 1";
                var dbId = await connection.QueryFirstOrDefaultSafeAsync<int?>(sql, new { Name = databaseName });
                return dbId ?? 0;
            }
            catch (Exception ex)
            {
                throw new DatabaseException($"Error getting database ID for name '{databaseName}'", ex);
            }
        }

        /// <summary>
        /// Checks if a database exists by its ID
        /// </summary>
        public static async Task<bool> DatabaseExistsAsync(this IDbConnection connection, int databaseId)
        {
            try
            {
                const string sql = "SELECT COUNT(1) FROM Databases WHERE DatabaseID = @DatabaseID AND IsActive = 1";
                var count = await connection.ExecuteScalarSafeAsync<int>(sql, new { DatabaseID = databaseId });
                return count > 0;
            }
            catch (Exception ex)
            {
                throw new DatabaseException($"Error checking if database exists: {databaseId}", ex);
            }
        }

        /// <summary>
        /// Gets a database by its ID
        /// </summary>
        public static async Task<DynamicDashboardCommon.Models.Database> GetDatabaseByIdAsync(this IDbConnection connection, int databaseId)
        {
            try
            {
                const string sql = @"
                    SELECT 
                        d.*, 
                        dt.TypeName as DatabaseTypeName
                    FROM Databases d
                    LEFT JOIN DatabaseTypes dt ON d.TypeID = dt.TypeID
                    WHERE d.DatabaseID = @DatabaseID";

                return await connection.QueryFirstOrDefaultSafeAsync<DynamicDashboardCommon.Models.Database>(
                    sql, new { DatabaseID = databaseId });
            }
            catch (Exception ex)
            {
                throw new DatabaseException($"Error getting database by ID: {databaseId}", ex);
            }
        }

        /// <summary>
        /// Gets a database by its name
        /// </summary>
        public static async Task<DynamicDashboardCommon.Models.Database> GetDatabaseByNameAsync(this IDbConnection connection, string databaseName)
        {
            try
            {
                const string sql = @"
                    SELECT 
                        d.*, 
                        dt.TypeName as DatabaseTypeName
                    FROM Databases d
                    LEFT JOIN DatabaseTypes dt ON d.TypeID = dt.TypeID
                    WHERE d.Name = @Name";

                return await connection.QueryFirstOrDefaultSafeAsync<DynamicDashboardCommon.Models.Database>(
                    sql, new { Name = databaseName });
            }
            catch (Exception ex)
            {
                throw new DatabaseException($"Error getting database by name: {databaseName}", ex);
            }
        }

        #endregion

        #region Transaction Helpers

        /// <summary>
        /// Executes multiple operations within a transaction
        /// </summary>
        public static async Task<TResult> ExecuteInTransactionAsync<TResult>(
            this IDbConnection connection, Func<IDbTransaction, Task<TResult>> action)
        {
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            using var transaction = connection.BeginTransaction();
            try
            {
                var result = await action(transaction);
                transaction.Commit();
                return result;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        /// <summary>
        /// Executes multiple operations within a transaction with no return value
        /// </summary>
        public static async Task ExecuteInTransactionAsync(
            this IDbConnection connection, Func<IDbTransaction, Task> action)
        {
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            using var transaction = connection.BeginTransaction();
            try
            {
                await action(transaction);
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        #endregion

        #region Bulk Operations

        /// <summary>
        /// Bulk inserts data into a table
        /// Note: Implementation is database-specific and will use the most efficient approach for each DB type
        /// </summary>
        public static async Task<int> BulkInsertAsync<T>(
            this IDbConnection connection, string tableName, IEnumerable<T> data,
            IDbTransaction transaction = null, int batchSize = 1000)
        {
            // This is a simplified implementation
            // In a real-world scenario, this would use SqlBulkCopy for SQL Server, 
            // MySqlBulkLoader for MySQL, etc.

            int totalInserted = 0;
            var dataList = data as List<T> ?? new List<T>(data);

            if (dataList.Count == 0)
                return 0;

            // Get property names from first item
            var properties = typeof(T).GetProperties();
            var columnNames = properties.Select(p => p.Name).ToList();

            // Create batches
            var batches = new List<List<T>>();
            for (int i = 0; i < dataList.Count; i += batchSize)
            {
                batches.Add(dataList.Skip(i).Take(batchSize).ToList());
            }

            foreach (var batch in batches)
            {
                // Create bulk insert SQL
                var valuePlaceholders = new List<string>();
                var parameters = new DynamicParameters();
                int itemIndex = 0;

                foreach (var item in batch)
                {
                    var valueClause = new List<string>();

                    foreach (var prop in properties)
                    {
                        string paramName = $"@p{itemIndex}_{prop.Name}";
                        valueClause.Add(paramName);
                        parameters.Add(paramName, prop.GetValue(item));
                    }

                    valuePlaceholders.Add($"({string.Join(", ", valueClause)})");
                    itemIndex++;
                }

                string sql = $@"
                    INSERT INTO {tableName} 
                    ({string.Join(", ", columnNames)})
                    VALUES 
                    {string.Join(",\n", valuePlaceholders)}";

                totalInserted += await connection.ExecuteAsync(sql, parameters, transaction);
            }

            return totalInserted;
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Executes a function with a database connection
        /// </summary>
        public static async Task<TResult> ExecuteWithConnectionAsync<TResult>(
            this IDbConnection connection, Func<IDbConnection, Task<TResult>> action)
        {
            bool wasOpen = connection.State == ConnectionState.Open;

            try
            {
                if (!wasOpen)
                    connection.Open();

                return await action(connection);
            }
            finally
            {
                if (!wasOpen && connection.State == ConnectionState.Open)
                    connection.Close();
            }
        }

        /// <summary>
        /// Gets database schema information that works across different database types
        /// </summary>
        public static async Task<IEnumerable<dynamic>> GetDatabaseSchemaAsync(this IDbConnection connection)
        {
            try
            {
                string sql;

                if (connection.GetType().Name.Contains("SqlConnection"))
                {
                    // SQL Server
                    sql = @"
                        SELECT 
                            t.TABLE_NAME,
                            c.COLUMN_NAME,
                            c.DATA_TYPE,
                            c.IS_NULLABLE,
                            CASE WHEN pk.COLUMN_NAME IS NOT NULL THEN 1 ELSE 0 END AS IS_PRIMARY_KEY
                        FROM 
                            INFORMATION_SCHEMA.TABLES t
                        INNER JOIN 
                            INFORMATION_SCHEMA.COLUMNS c ON c.TABLE_NAME = t.TABLE_NAME
                        LEFT JOIN 
                            (
                                SELECT ku.TABLE_NAME, ku.COLUMN_NAME
                                FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                                INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE ku ON ku.CONSTRAINT_NAME = tc.CONSTRAINT_NAME
                                WHERE tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
                            ) pk ON pk.TABLE_NAME = t.TABLE_NAME AND pk.COLUMN_NAME = c.COLUMN_NAME
                        WHERE 
                            t.TABLE_TYPE = 'BASE TABLE'
                        ORDER BY 
                            t.TABLE_NAME, c.ORDINAL_POSITION";
                }
                else if (connection.GetType().Name.Contains("MySqlConnection"))
                {
                    // MySQL
                    sql = @"
                        SELECT 
                            t.TABLE_NAME,
                            c.COLUMN_NAME,
                            c.DATA_TYPE,
                            c.IS_NULLABLE,
                            CASE WHEN c.COLUMN_KEY = 'PRI' THEN 1 ELSE 0 END AS IS_PRIMARY_KEY
                        FROM 
                            INFORMATION_SCHEMA.TABLES t
                        INNER JOIN 
                            INFORMATION_SCHEMA.COLUMNS c ON c.TABLE_NAME = t.TABLE_NAME
                        WHERE 
                            t.TABLE_SCHEMA = DATABASE() AND
                            t.TABLE_TYPE = 'BASE TABLE'
                        ORDER BY 
                            t.TABLE_NAME, c.ORDINAL_POSITION";
                }
                else if (connection.GetType().Name.Contains("OracleConnection"))
                {
                    // Oracle
                    sql = @"
                        SELECT 
                            t.TABLE_NAME,
                            c.COLUMN_NAME,
                            c.DATA_TYPE,
                            CASE WHEN c.NULLABLE = 'Y' THEN 'YES' ELSE 'NO' END AS IS_NULLABLE,
                            CASE WHEN pk.COLUMN_NAME IS NOT NULL THEN 1 ELSE 0 END AS IS_PRIMARY_KEY
                        FROM 
                            USER_TABLES t
                        INNER JOIN 
                            USER_TAB_COLUMNS c ON c.TABLE_NAME = t.TABLE_NAME
                        LEFT JOIN 
                            (
                                SELECT ucc.TABLE_NAME, ucc.COLUMN_NAME
                                FROM USER_CONSTRAINTS uc
                                INNER JOIN USER_CONS_COLUMNS ucc ON ucc.CONSTRAINT_NAME = uc.CONSTRAINT_NAME
                                WHERE uc.CONSTRAINT_TYPE = 'P'
                            ) pk ON pk.TABLE_NAME = t.TABLE_NAME AND pk.COLUMN_NAME = c.COLUMN_NAME
                        ORDER BY 
                            t.TABLE_NAME, c.COLUMN_ID";
                }
                else
                {
                    throw new NotSupportedException($"Database type {connection.GetType().Name} not supported for schema retrieval");
                }

                return await connection.QueryAsync(sql);
            }
            catch (Exception ex)
            {
                throw new DatabaseException($"Error retrieving database schema: {ex.Message}", ex);
            }
        }

        #endregion

        #region Metadata Helpers

        /// <summary>
        /// Gets tables for a database by ID
        /// </summary>
        public static async Task<IEnumerable<DynamicDashboardCommon.Models.Table>> GetTablesByDatabaseIdAsync(
            this IDbConnection connection, int databaseId)
        {
            try
            {
                const string sql = "SELECT * FROM Tables WHERE DatabaseID = @DatabaseID";
                return await connection.QuerySafeAsync<DynamicDashboardCommon.Models.Table>(sql, new { DatabaseID = databaseId });
            }
            catch (Exception ex)
            {
                throw new DatabaseException($"Error retrieving tables for database: {databaseId}", ex);
            }
        }

        /// <summary>
        /// Gets columns for a table by ID
        /// </summary>
        public static async Task<IEnumerable<DynamicDashboardCommon.Models.Column>> GetColumnsByTableIdAsync(
            this IDbConnection connection, int tableId)
        {
            try
            {
                const string sql = "SELECT * FROM Columns WHERE TableID = @TableID";
                return await connection.QuerySafeAsync<DynamicDashboardCommon.Models.Column>(sql, new { TableID = tableId });
            }
            catch (Exception ex)
            {
                throw new DatabaseException($"Error retrieving columns for table: {tableId}", ex);
            }
        }

        /// <summary>
        /// Gets relationships for a table by ID
        /// </summary>
        public static async Task<IEnumerable<DynamicDashboardCommon.Models.Relationship>> GetRelationshipsByTableIdAsync(
            this IDbConnection connection, int tableId)
        {
            try
            {
                const string sql = "SELECT * FROM Relationships WHERE TableID = @TableID OR RelatedTableID = @TableID";
                return await connection.QuerySafeAsync<DynamicDashboardCommon.Models.Relationship>(sql, new { TableID = tableId });
            }
            catch (Exception ex)
            {
                throw new DatabaseException($"Error retrieving relationships for table: {tableId}", ex);
            }
        }

        #endregion
    }
}