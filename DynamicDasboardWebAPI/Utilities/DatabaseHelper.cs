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
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            if (string.IsNullOrWhiteSpace(sql)) throw new ArgumentException("SQL query cannot be empty", nameof(sql));

            try
            {
                return await connection.QueryAsync<T>(sql, param, transaction, commandTimeout, commandType);
            }
            catch (Exception ex)
            {
                throw new DatabaseException($"Error executing query: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Executes a command safely and returns the number of affected rows
        /// </summary>
        public static async Task<int> ExecuteSafeAsync(this IDbConnection connection, string sql, object param = null,
            IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            if (string.IsNullOrWhiteSpace(sql)) throw new ArgumentException("SQL command cannot be empty", nameof(sql));

            try
            {
                return await connection.ExecuteAsync(sql, param, transaction, commandTimeout, commandType);
            }
            catch (Exception ex)
            {
                throw new DatabaseException($"Error executing command: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Executes a query safely and returns the first result or default value
        /// </summary>
        public static async Task<T> QueryFirstOrDefaultSafeAsync<T>(this IDbConnection connection, string sql, object param = null,
            IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            if (string.IsNullOrWhiteSpace(sql)) throw new ArgumentException("SQL query cannot be empty", nameof(sql));

            try
            {
                return await connection.QueryFirstOrDefaultAsync<T>(sql, param, transaction, commandTimeout, commandType);
            }
            catch (Exception ex)
            {
                throw new DatabaseException($"Error executing query: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Executes a query safely and returns a single result
        /// </summary>
        public static async Task<T> QuerySingleOrDefaultSafeAsync<T>(this IDbConnection connection, string sql, object param = null,
            IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            if (string.IsNullOrWhiteSpace(sql)) throw new ArgumentException("SQL query cannot be empty", nameof(sql));

            try
            {
                return await connection.QuerySingleOrDefaultAsync<T>(sql, param, transaction, commandTimeout, commandType);
            }
            catch (Exception ex)
            {
                throw new DatabaseException($"Error executing query: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Executes a scalar query safely and returns the result
        /// </summary>
        public static async Task<T> ExecuteScalarSafeAsync<T>(this IDbConnection connection, string sql, object param = null,
            IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            if (string.IsNullOrWhiteSpace(sql)) throw new ArgumentException("SQL query cannot be empty", nameof(sql));

            try
            {
                return await connection.ExecuteScalarAsync<T>(sql, param, transaction, commandTimeout, commandType);
            }
            catch (Exception ex)
            {
                throw new DatabaseException($"Error executing scalar query: {ex.Message}", ex);
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
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            if (string.IsNullOrWhiteSpace(sql)) throw new ArgumentException("SQL query cannot be empty", nameof(sql));

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
            if (results == null) throw new ArgumentNullException(nameof(results));

            var dictResults = new List<Dictionary<string, object>>();

            foreach (var item in results)
            {
                var dict = new Dictionary<string, object>();

                if (item is IDictionary<string, object> dynamicItem)
                {
                    foreach (var prop in dynamicItem)
                    {
                        dict[prop.Key] = prop.Value;
                    }
                }
                else
                {
                    // Handle non-dynamic objects using reflection
                    foreach (var prop in item.GetType().GetProperties())
                    {
                        dict[prop.Name] = prop.GetValue(item);
                    }
                }

                dictResults.Add(dict);
            }

            return dictResults;
        }

        #endregion

        #region Transaction Helpers

        /// <summary>
        /// Executes multiple operations within a transaction
        /// </summary>
        public static async Task<TResult> ExecuteInTransactionAsync<TResult>(
            this IDbConnection connection, Func<IDbTransaction, Task<TResult>> action)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            if (action == null) throw new ArgumentNullException(nameof(action));

            bool wasOpen = connection.State == ConnectionState.Open;

            try
            {
                if (!wasOpen)
                    connection.Open();

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
            finally
            {
                if (!wasOpen && connection.State == ConnectionState.Open)
                    connection.Close();
            }
        }

        /// <summary>
        /// Executes multiple operations within a transaction with no return value
        /// </summary>
        public static async Task ExecuteInTransactionAsync(
            this IDbConnection connection, Func<IDbTransaction, Task> action)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            if (action == null) throw new ArgumentNullException(nameof(action));

            bool wasOpen = connection.State == ConnectionState.Open;

            try
            {
                if (!wasOpen)
                    connection.Open();

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
            finally
            {
                if (!wasOpen && connection.State == ConnectionState.Open)
                    connection.Close();
            }
        }

        #endregion

        #region Application DB Helper Methods

        /// <summary>
        /// Gets a database ID by its name from the application database
        /// </summary>
        public static async Task<int> GetDatabaseIdByNameAsync(this IDbConnection connection, string databaseName)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            if (string.IsNullOrWhiteSpace(databaseName)) throw new ArgumentException("Database name cannot be empty", nameof(databaseName));

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
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            if (databaseId <= 0) throw new ArgumentException("Invalid database ID", nameof(databaseId));

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
        /// Gets a database by its ID from the application database
        /// </summary>
        public static async Task<Database> GetDatabaseByIdAsync(this IDbConnection connection, int databaseId)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            if (databaseId <= 0) throw new ArgumentException("Invalid database ID", nameof(databaseId));

            try
            {
                const string sql = @"
                    SELECT 
                        d.*, 
                        dt.TypeName as DatabaseTypeName
                    FROM Databases d
                    LEFT JOIN DatabaseTypes dt ON d.TypeID = dt.TypeID
                    WHERE d.DatabaseID = @DatabaseID";

                return await connection.QueryFirstOrDefaultSafeAsync<Database>(
                    sql, new { DatabaseID = databaseId });
            }
            catch (Exception ex)
            {
                throw new DatabaseException($"Error getting database by ID: {databaseId}", ex);
            }
        }



        /// <summary>
        /// Gets all databases with their type names from the application database
        /// </summary>
        public static async Task<IEnumerable<Database>> GetAllDatabasesAsync(this IDbConnection connection)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));

            try
            {
                const string sql = @"
            SELECT 
                d.*, 
                dt.TypeName as DatabaseTypeName
            FROM Databases d
            LEFT JOIN DatabaseTypes dt ON d.TypeID = dt.TypeID";

                return await connection.QuerySafeAsync<Database>(sql);
            }
            catch (Exception ex)
            {
                throw new DatabaseException($"Error retrieving all databases: {ex.Message}", ex);
            }
        }
        /// <summary>
        /// Gets a database by its name from the application database
        /// </summary>
        public static async Task<Database> GetDatabaseByNameAsync(this IDbConnection connection, string databaseName)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            if (string.IsNullOrWhiteSpace(databaseName)) throw new ArgumentException("Database name cannot be empty", nameof(databaseName));

            try
            {
                const string sql = @"
                    SELECT 
                        d.*, 
                        dt.TypeName as DatabaseTypeName
                    FROM Databases d
                    LEFT JOIN DatabaseTypes dt ON d.TypeID = dt.TypeID
                    WHERE d.Name = @Name";

                return await connection.QueryFirstOrDefaultSafeAsync<Database>(
                    sql, new { Name = databaseName });
            }
            catch (Exception ex)
            {
                throw new DatabaseException($"Error getting database by name: {databaseName}", ex);
            }
        }

        /// <summary>
        /// Gets database type name by its ID from the application database
        /// </summary>
     

        #endregion

        #region Metadata Helpers

        /// <summary>
        /// Gets tables for a database by ID
        /// </summary>
        public static async Task<IEnumerable<Table>> GetTablesByDatabaseIdAsync(
            this IDbConnection connection, int databaseId)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            if (databaseId <= 0) throw new ArgumentException("Invalid database ID", nameof(databaseId));

            try
            {
                const string sql = "SELECT * FROM Tables WHERE DatabaseID = @DatabaseID";
                return await connection.QuerySafeAsync<Table>(sql, new { DatabaseID = databaseId });
            }
            catch (Exception ex)
            {
                throw new DatabaseException($"Error retrieving tables for database: {databaseId}", ex);
            }
        }

        /// <summary>
        /// Gets columns for a table by ID
        /// </summary>
        public static async Task<IEnumerable<Column>> GetColumnsByTableIdAsync(
            this IDbConnection connection, int tableId)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            if (tableId <= 0) throw new ArgumentException("Invalid table ID", nameof(tableId));

            try
            {
                const string sql = "SELECT * FROM Columns WHERE TableID = @TableID";
                return await connection.QuerySafeAsync<Column>(sql, new { TableID = tableId });
            }
            catch (Exception ex)
            {
                throw new DatabaseException($"Error retrieving columns for table: {tableId}", ex);
            }
        }

        /// <summary>
        /// Gets relationships for a table by ID
        /// </summary>
        public static async Task<IEnumerable<Relationship>> GetRelationshipsByTableIdAsync(
            this IDbConnection connection, int tableId)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            if (tableId <= 0) throw new ArgumentException("Invalid table ID", nameof(tableId));

            try
            {
                const string sql = "SELECT * FROM Relationships WHERE TableID = @TableID OR RelatedTableID = @TableID";
                return await connection.QuerySafeAsync<Relationship>(sql, new { TableID = tableId });
            }
            catch (Exception ex)
            {
                throw new DatabaseException($"Error retrieving relationships for table: {tableId}", ex);
            }
        }

        #endregion

        #region Bulk Operations

        /// <summary>
        /// Bulk inserts data into a table
        /// </summary>
        public static async Task<int> BulkInsertAsync<T>(
            this IDbConnection connection, string tableName, IEnumerable<T> data,
            IDbTransaction transaction = null, int batchSize = 1000)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentException("Table name cannot be empty", nameof(tableName));
            if (data == null) throw new ArgumentNullException(nameof(data));

            int totalInserted = 0;
            var dataList = data as List<T> ?? new List<T>(data);

            if (dataList.Count == 0)
                return 0;

            try
            {
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
            catch (Exception ex)
            {
                throw new DatabaseException($"Error performing bulk insert into {tableName}: {ex.Message}", ex);
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Executes a function with a database connection
        /// </summary>
        public static async Task<TResult> ExecuteWithConnectionAsync<TResult>(
            this IDbConnection connection, Func<IDbConnection, Task<TResult>> action)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            if (action == null) throw new ArgumentNullException(nameof(action));

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
            if (connection == null) throw new ArgumentNullException(nameof(connection));

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

        /// <summary>
        /// Updates the LastConnectionStatus and LastTransactionDate for a database
        /// </summary>
        public static async Task UpdateConnectionStatusAsync(this IDbConnection connection, int databaseId, bool status)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            if (databaseId <= 0) throw new ArgumentException("Invalid database ID", nameof(databaseId));

            try
            {
                const string query = @"
                    UPDATE Databases 
                    SET LastConnectionStatus = @Status, LastTransactionDate = GETDATE()
                    WHERE DatabaseID = @DatabaseID";

                await connection.ExecuteSafeAsync(query, new
                {
                    DatabaseID = databaseId,
                    Status = status
                });
            }
            catch (Exception ex)
            {
                // Log but don't throw as this is a non-critical operation
                // If we have a logger, we could log here
                // _logger?.LogError(ex, "Error updating connection status for database: {DatabaseId}", databaseId);
            }
        }

        /// <summary>
        /// Sanitizes a SQL identifier to help prevent SQL injection
        /// </summary>
        public static string SanitizeSqlIdentifier(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
                return string.Empty;

            // Replace any non-alphanumeric characters except underscores with empty string
            return System.Text.RegularExpressions.Regex.Replace(identifier, "[^a-zA-Z0-9_]", "");
        }

        #endregion
    }
}