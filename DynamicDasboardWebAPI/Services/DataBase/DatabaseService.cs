using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Data;
using Dapper;
using Microsoft.Extensions.Logging;
using DynamicDashboardCommon.Models;
using DynamicDasboardWebAPI.Repositories;
using DynamicDasboardWebAPI.Utilities;
using System.Linq;
using DynamicDashboardCommon.Enums;
using Microsoft.AspNetCore.Mvc;
using DynamicDashboardCommon.Helper;


namespace DynamicDasboardWebAPI.Services
{
    /// <summary>
    /// Service for managing database operations including connections, metadata, and queries.
    /// </summary>
    public class DatabaseService : IDatabaseService
    {
        private readonly DatabaseRepository objDataBaseRepository;
        private readonly TableRepository objTablereposiroty;
        private readonly ColumnRepository objColumnreposiroty;
        private readonly RelationshipService objRelationshipService;
        private readonly ConcurrentDictionary<string, int> _typeIdCache = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseService"/> class.
        /// </summary>
        /// <param name="repository">The repository for database operations.</param>
        /// <param name="tableRepository">The repository for table operations.</param>
        /// <param name="columnRepository">The repository for column operations.</param>
        /// <param name="connectionFactory">The factory for creating database connections.</param>
        /// <param name="logger">The logger for service operations.</param>
        public DatabaseService(
            DatabaseRepository repository,
            TableRepository tableRepository,
            ColumnRepository columnRepository,
            RelationshipService relationshipService)
        {
            objDataBaseRepository = repository ?? throw new ArgumentNullException(nameof(repository));
            objTablereposiroty = tableRepository ?? throw new ArgumentNullException(nameof(tableRepository));
            objRelationshipService = relationshipService ?? throw new ArgumentNullException(nameof(relationshipService));
            objColumnreposiroty = columnRepository ?? throw new ArgumentNullException(nameof(columnRepository));


        }

        /// <summary>
        /// Retrieves all databases from the system.
        /// </summary>
        /// <returns>A collection of all databases.</returns>
        public async Task<IEnumerable<Database>> GetAllDatabasesAsync()
        {
            try
            {
                var databases = await objDataBaseRepository.GetAllDatabasesAsync();

                // Enrich with type names if needed
                foreach (var db in databases)
                {
                    if (string.IsNullOrEmpty(db.DatabaseTypeName))
                    {
                        db.DatabaseTypeName = await GetDatabaseTypeNameAsync(db.TypeID);
                    }
                }

                return databases;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        /// <summary>
        /// Gets a database by ID.
        /// </summary>
        /// <param name="databaseId">The ID of the database to retrieve.</param>
        /// <returns>The database with the specified ID, or null if not found.</returns>
        public async Task<Database> GetDatabaseByIdAsync(int databaseId)
        {
            try
            {
                // Check cache first
                var cacheKey = $"Database_{databaseId}";
                var cached = CacheHelper.Get<Database>(cacheKey);
                if (cached != null)
                {
                    return cached;
                }
                var database = await objDataBaseRepository.GetDatabaseByIdAsync(databaseId);
                return database;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        /// <summary>
        /// Adds a new database to the system.
        /// </summary>
        /// <param name="database">The database to add.</param>
        /// <returns>The ID of the newly added database.</returns>
        public async Task<int> AddDatabaseAsync(Database database)
        {
            if (database == null)
                return 0;

            try
            {
                // Validate required fields
                if (!string.IsNullOrWhiteSpace(database.Name) && !string.IsNullOrWhiteSpace(database.ServerAddress) && !string.IsNullOrEmpty(database.FriendlyName))
                {
                    
                    // Set initial values for new database
                    database.CreatedAt = DateTime.UtcNow;
                    database.IsActive = true;



                    int databaseId = await objDataBaseRepository.AddDatabaseAsync(database);

                    await CacheHelper.AddOrUpdateAsync($"Database_{database.DatabaseID}", database);

                    return databaseId;
                }
                else
                {
                    return 0;
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        /// <summary>
        /// Updates an existing database in the system.
        /// </summary>
        /// <param name="database">The database to update.</param>
        /// <returns>The number of affected rows.</returns>
        public async Task<int> UpdateDatabaseAsync(Database database)
        {
            if (database == null)
                return 0;

            try
            {
                if (!string.IsNullOrWhiteSpace(database.Name) && !string.IsNullOrWhiteSpace(database.ServerAddress) && !string.IsNullOrEmpty(database.FriendlyName))
                {     // Validate required fields
                    int result = await objDataBaseRepository.UpdateDatabaseAsync(database);

                    await CacheHelper.AddOrUpdateAsync($"Database_{database.DatabaseID}", database);

                    return result;
                }
                else
                {
                    return 0;
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        /// <summary>
        /// Deletes a database from the system.
        /// </summary>
        /// <param name="databaseId">The ID of the database to delete.</param>
        /// <returns>The number of affected rows.</returns>
        public async Task<int> DeleteDatabaseAsync(int databaseId)
        {
            try
            {
                int result = await objDataBaseRepository.DeleteDatabaseAsync(databaseId);
                return result;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        /// <summary>
        /// Tests a database connection using the provided connection details.
        /// </summary>
        /// <param name="database">The database connection to test.</param>
        /// <returns>True if the connection was successful; otherwise, false.</returns>
        public async Task<bool> TestConnectionAsync(Database database)
        {
            if (database == null)
            {
                return false;
            }

            try
            {
                if (database.DatabaseID != 0)
                {
                    database = await GetDatabaseByIdAsync(database.DatabaseID);
                }
                else
                {
                    // Convert request to a temporary database object
                    database = new Database
                    {
                        Name = database.Name,
                        DatabaseID = database.DatabaseID,
                        ServerAddress = database.ServerAddress,
                        FriendlyName = database.FriendlyName,
                        TypeID = database.TypeID,
                        Port = database.Port,
                        Username = database.Username,
                        EncryptedCredentials = database.EncryptedCredentials // Note: In production, this should be encrypted //temp
                    };
                }
                // Test connection
                bool isSuccess = await objDataBaseRepository.TestConnectionAsync(database);
                return isSuccess;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        /// <summary>
        /// Gets a list of supported database types.
        /// </summary>
        /// <returns>A list of supported database types.</returns>
        public async Task<List<DatabaseType>> GetSupportedDatabaseTypesAsync()
        {
            try
            {

                // Get types from repository
                return await objDataBaseRepository.GetSupportedDatabaseTypesAsync();
            }
            catch (Exception ex)
            {

                // Fallback only if database query fails
                throw;
            }
        }

        /// <summary>
        /// Gets database metadata for the specified database.
        /// </summary>
        /// <param name="databaseId">The ID of the database.</param>
        /// <returns>True if metadata was retrieved successfully; otherwise, false.</returns>
        public async Task<bool> GetDatabaseMetadataAsync(int databaseId)
        {
            try
            {

                return true; //temp
            }
            catch (Exception ex)
            {
                throw;
            }
        }



        /// <summary>
        /// Gets all database types.
        /// </summary>
        /// <returns>A collection of database types.</returns>
        public async Task<IEnumerable<DatabaseType>> GetAllDatabaseTypesAsync()
        {
            try
            {
                return await objDataBaseRepository.GetSupportedDatabaseTypesAsync();
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        /// <summary>
        /// Gets the database type name for the specified ID.
        /// </summary>
        /// <param name="typeId">The type ID.</param>
        /// <returns>The database type name.</returns>
        public async Task<string> GetDatabaseTypeNameAsync(int typeId)
        {
            try
            {
                // Check cache first (inverted lookup)
                foreach (var kvp in _typeIdCache)
                {
                    if (kvp.Value == typeId)
                    {
                        return kvp.Key;
                    }
                }

                // If not in cache, get from repository
                string typeName = await objDataBaseRepository.GetDatabaseTypeNameAsync(typeId);

                // If found, add to cache
                if (!string.IsNullOrEmpty(typeName))
                {
                    _typeIdCache.TryAdd(typeName.ToLowerInvariant(), typeId);
                }

                return typeName;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        /// <summary>
        /// Retrieves the database schema from a user's database.
        /// </summary>
        /// <param name="databaseId">The ID of the database.</param>
        /// <returns>A collection of tables and columns from the schema.</returns>
        //public async Task<IEnumerable<SchemaTableDto>> RetrieveDatabaseSchemaAsync(int databaseId)
        //{
        //    try
        //    {

        //        // Check if schema already exists in our application database
        //        var existingTables = await _tableRepository.GetTablesByDatabaseIdAsync(databaseId);
        //        if (existingTables.Any())
        //        {
        //            // Return the existing schema
        //            return await GetSavedSchemaAsync(databaseId);
        //        }

        //        // Get the database connection details
        //        var database = await GetDatabaseByIdAsync(databaseId);
        //        if (database == null)
        //            throw new ArgumentException($"Database with ID {databaseId} not found");

        //        // Create a connection to the target database

        //        //using var connection = await _DBRepository.CreateOpenConnectionAsync(databaseId);

        //        // Retrieve the schema - this uses the polymorphic method in DatabaseHelper that works across database types
        //        var schemaData = await _DBRepository.GetDatabaseSchemaAsync();

        //        // Convert the schema data to DTOs
        //        var result = ConvertSchemaToDto(schemaData);

        //        return result;
        //    }
        //    catch (Exception ex)
        //    {
        //        throw;
        //    }
        //}

        /// <summary>
        /// Retrieves schema information that has been saved in the application database.
        /// </summary>
        /// <param name="databaseId">The ID of the database.</param>
        /// <returns>A collection of tables and columns from the saved schema.</returns>
        private async Task<IEnumerable<SchemaTableDto>> GetSavedSchemaAsync(int databaseId)
        {
            var result = new List<SchemaTableDto>();

            try
            {
                // Get all tables for the database
                var tables = await objTablereposiroty.GetTablesByDatabaseIdAsync(databaseId);

                foreach (var table in tables)
                {
                    // Get all columns for the table
                    var columns = await objColumnreposiroty.GetColumnsByTableIdAsync(table.TableID);

                    var tableDto = new SchemaTableDto
                    {
                        TableName = table.DBTableName,
                        AdminTableName = table.AdminTableName,
                        AdminDescription = table.AdminDescription,
                        Columns = columns.Select(c => new SchemaColumnDto
                        {
                            ColumnName = c.DBColumnName,
                            AdminColumnName = c.AdminColumnName,
                            DataType = c.DataType,
                            IsNullable = c.IsNullable,
                            IsPrimary = false, // Would need to retrieve from database
                            IsForeignKey = false, // Would need to retrieve from database
                            AdminDescription = c.AdminDescription
                        }).ToList()
                    };

                    result.Add(tableDto);
                }

                return result;
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Converts raw schema data to DTOs.
        /// </summary>
        /// <param name="schemaData">Raw schema data from the database.</param>
        /// <returns>A collection of table DTOs.</returns>
        private IEnumerable<SchemaTableDto> ConvertSchemaToDto(IEnumerable<dynamic> schemaData)
        {
            // Initialize the dictionary to store table DTOs
            var tableDtos = new Dictionary<string, SchemaTableDto>();

            try
            {
                foreach (var item in schemaData)
                {
                    var tableName = item.TABLE_NAME.ToString();
                    var columnName = item.COLUMN_NAME.ToString();
                    var dataType = item.DATA_TYPE.ToString();
                    var isNullable = item.IS_NULLABLE.ToString().Equals("YES", StringComparison.OrdinalIgnoreCase);
                    var isPrimary = Convert.ToBoolean(item.IS_PRIMARY_KEY);

                    // Check if the table already exists in the dictionary
                    if (!tableDtos.TryGetValue(tableName, out SchemaTableDto tableDto))
                    {
                        // Create a new table DTO if it doesn't exist
                        tableDto = new SchemaTableDto
                        {
                            TableName = tableName,
                            AdminTableName = tableName, // Default to the database table name
                            AdminDescription = "", // Empty initially
                            Columns = new List<SchemaColumnDto>()
                        };

                        // Add the new table DTO to the dictionary
                        tableDtos[tableName] = tableDto;
                    }

                    // Add the column to the table's column list
                    tableDto.Columns.Add(new SchemaColumnDto
                    {
                        ColumnName = columnName,
                        AdminColumnName = columnName, // Default to the database column name
                        DataType = dataType,
                        IsNullable = isNullable,
                        IsPrimary = isPrimary,
                        IsForeignKey = false, // Additional logic required to determine foreign keys
                        AdminDescription = "" // Empty initially
                    });
                }

                // Return the values of the dictionary (collection of SchemaTableDto)
                return tableDtos.Values;
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Saves database schema to the application database with proper handling of tables, columns, and relationships.
        /// </summary>
        /// <param name="databaseId">The ID of the database.</param>
        /// <param name="schema">The schema to save.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        //public async Task SaveDatabaseSchemaAsync(int databaseId, IEnumerable<SchemaTableDto> schema)
        //{
        //    try
        //    {

        //        // Using a transaction to ensure all-or-nothing updates
        //        using (var connection = _connectionFactory.CreateConnection(databaseId))
        //        {
        //            // Begin a transaction
        //            using (var transaction = connection.BeginTransaction())
        //            {
        //                try
        //                {
        //                    // Get all existing tables and relationships
        //                    var existingTables = (await _tableRepository.GetTablesByDatabaseIdAsync(databaseId)).ToList();
        //                    var existingTableDict = existingTables.ToDictionary(t => t.DBTableName, StringComparer.OrdinalIgnoreCase);

        //                    // Track processed tables and their columns
        //                    var processedTableIds = new HashSet<int>();
        //                    var tableNameToIdMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        //                    var columnMapping = new Dictionary<string, Dictionary<string, int>>(StringComparer.OrdinalIgnoreCase);

        //                    // Process tables in schema
        //                    foreach (var tableDto in schema)
        //                    {
        //                        // Check if table exists
        //                        if (existingTableDict.TryGetValue(tableDto.TableName, out var existingTable))
        //                        {
        //                            processedTableIds.Add(existingTable.TableID);
        //                            tableNameToIdMap[tableDto.TableName] = existingTable.TableID;

        //                            // Update table properties if changed
        //                            bool tableChanged =
        //                                existingTable.AdminTableName != tableDto.AdminTableName ||
        //                                existingTable.AdminDescription != tableDto.AdminDescription;

        //                            if (tableChanged)
        //                            {
        //                                existingTable.AdminTableName = tableDto.AdminTableName;
        //                                existingTable.AdminDescription = tableDto.AdminDescription;
        //                                await _tableRepository.UpdateTableAsync(existingTable);
        //                            }

        //                            // Process columns for this table
        //                            await ProcessTableColumns(existingTable.TableID, tableDto.Columns, columnMapping, transaction);
        //                        }
        //                        else
        //                        {
        //                            // Create new table
        //                            var newTable = new Table
        //                            {
        //                                DatabaseID = databaseId,
        //                                DBTableName = tableDto.TableName,
        //                                AdminTableName = tableDto.AdminTableName,
        //                                AdminDescription = tableDto.AdminDescription
        //                            };

        //                            int newTableId = await _tableRepository.AddTableAsync(newTable);
        //                            processedTableIds.Add(newTableId);
        //                            tableNameToIdMap[tableDto.TableName] = newTableId;

        //                            // Add all columns for the new table
        //                            await ProcessTableColumns(newTableId, tableDto.Columns, columnMapping, transaction);
        //                        }
        //                    }

        //                    // Process relationships (if schema has relationship info)
        //                    await ProcessRelationships(databaseId, schema, tableNameToIdMap, columnMapping, transaction);

        //                    // Handle tables not in schema (optional deletion)
        //                    foreach (var existingTable in existingTables)
        //                    {
        //                        if (!processedTableIds.Contains(existingTable.TableID))
        //                        {
        //                            // Delete table not in schema
        //                            await _tableRepository.DeleteTableAsync(existingTable.TableID);
        //                        }
        //                    }

        //                    // Commit transaction
        //                    transaction.Commit();
        //                }
        //                catch (Exception ex)
        //                {
        //                    // Rollback on error
        //                    transaction.Rollback();
        //                    throw;
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        throw;
        //    }
        //}

        /// <summary>
        /// Processes columns for a table, updating existing ones and adding new ones.
        /// </summary>
        private async Task ProcessTableColumns(int tableId, IEnumerable<SchemaColumnDto> columnDtos,
            Dictionary<string, Dictionary<string, int>> columnMapping, IDbTransaction transaction)
        {
            try
            {
                // Get existing columns
                var existingColumns = await objColumnreposiroty.GetColumnsByTableIdAsync(tableId);
                var existingColumnDict = existingColumns.ToDictionary(c => c.DBColumnName, StringComparer.OrdinalIgnoreCase);
                var processedColumnIds = new HashSet<int>();
                var tableDictionary = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                // Process columns
                foreach (var columnDto in columnDtos)
                {
                    if (existingColumnDict.TryGetValue(columnDto.ColumnName, out var existingColumn))
                    {
                        processedColumnIds.Add(existingColumn.ColumnID);
                        tableDictionary[columnDto.ColumnName] = existingColumn.ColumnID;

                        // Update column if changed
                        bool columnChanged =
                            existingColumn.AdminColumnName != columnDto.AdminColumnName ||
                            existingColumn.AdminDescription != columnDto.AdminDescription ||
                            existingColumn.IsLookupColumn != columnDto.IsLookupColumn;

                        if (columnChanged)
                        {
                            existingColumn.AdminColumnName = columnDto.AdminColumnName;
                            existingColumn.AdminDescription = columnDto.AdminDescription;
                            existingColumn.IsLookupColumn = columnDto.IsLookupColumn;
                            await objColumnreposiroty.UpdateColumnAsync(existingColumn);
                        }
                    }
                    else
                    {
                        // Create new column
                        var newColumn = new Column
                        {
                            TableID = tableId,
                            DBColumnName = columnDto.ColumnName,
                            AdminColumnName = columnDto.AdminColumnName,
                            DataType = columnDto.DataType,
                            IsNullable = columnDto.IsNullable,
                            AdminDescription = columnDto.AdminDescription,
                            IsLookupColumn = columnDto.IsLookupColumn
                        };

                        int newColumnId = await objColumnreposiroty.AddColumnAsync(newColumn);
                        tableDictionary[columnDto.ColumnName] = newColumnId;
                    }
                }

                // Handle columns not in schema (delete)
                foreach (var existingColumn in existingColumns)
                {
                    if (!processedColumnIds.Contains(existingColumn.ColumnID))
                    {
                        await objColumnreposiroty.DeleteColumnAsync(existingColumn.ColumnID);
                    }
                }

                // Store column mapping for this table
                var tableInfo = await objTablereposiroty.GetTableByIdAsync(tableId);
                if (tableInfo != null)
                {
                    columnMapping[tableInfo.DBTableName] = tableDictionary;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Processes relationships between tables and columns.
        /// </summary>
        private async Task ProcessRelationships(int databaseId, IEnumerable<SchemaTableDto> schema,
            Dictionary<string, int> tableNameToIdMap, Dictionary<string, Dictionary<string, int>> columnMapping,
            IDbTransaction transaction)
        {
            // Get all existing relationships for all tables
            var allRelationships = new List<Relationship>();
            try
            {
                foreach (var tableId in tableNameToIdMap.Values)
                {
                    var tableRelationships = await objRelationshipService.GetRelationshipsByTableIdAsync(tableId);
                    allRelationships.AddRange(tableRelationships);
                }

                // Track processed relationships
                var processedRelationshipIds = new HashSet<int>();

                // Extract and process relationships from schema
                // We need to detect if the schema includes relationship information
                bool hasRelationshipInfo = schema.Any(t => t.GetType().GetProperty("Relationships") != null);

                if (hasRelationshipInfo)
                {
                    foreach (dynamic tableDto in schema)
                    {
                        if (tableDto.Relationships != null)
                        {
                            string sourceTableName = tableDto.TableName;

                            if (!tableNameToIdMap.TryGetValue(sourceTableName, out int sourceTableId))
                            {
                                continue;
                            }

                            foreach (dynamic rel in tableDto.Relationships)
                            {
                                // Extract relationship data
                                string targetTableName = rel.TargetTable;
                                string sourceColumnName = rel.SourceColumn;
                                string targetColumnName = rel.TargetColumn;
                                string relationType = rel.RelationshipType;

                                if (!tableNameToIdMap.TryGetValue(targetTableName, out int targetTableId) ||
                                    !columnMapping.ContainsKey(sourceTableName) ||
                                    !columnMapping.ContainsKey(targetTableName) ||
                                    !columnMapping[sourceTableName].TryGetValue(sourceColumnName, out int sourceColumnId) ||
                                    !columnMapping[targetTableName].TryGetValue(targetColumnName, out int targetColumnId))
                                {
                                    continue;
                                }

                                // Check for existing relationship
                                var existingRel = allRelationships.FirstOrDefault(r =>
                                    r.TableID == sourceTableId &&
                                    r.ColumnID == sourceColumnId &&
                                    r.RelatedTableID == targetTableId &&
                                    r.RelatedColumnID == targetColumnId);

                                if (existingRel != null)
                                {
                                    processedRelationshipIds.Add(existingRel.RelationshipID);

                                    // Update if needed
                                    if (existingRel.RelationshipType != relationType)
                                    {
                                        existingRel.RelationshipType = relationType;
                                        await objRelationshipService.UpdateRelationshipAsync(existingRel);
                                    }
                                }
                                else
                                {
                                    // Create new relationship
                                    var newRelationship = new Relationship
                                    {
                                        TableID = sourceTableId,
                                        ColumnID = sourceColumnId,
                                        RelatedTableID = targetTableId,
                                        RelatedColumnID = targetColumnId,
                                        RelationshipType = relationType,
                                        Description = $"Relationship from {sourceTableName}.{sourceColumnName} to {targetTableName}.{targetColumnName}",
                                        IsEnforced = false,
                                        CreatedAt = DateTime.UtcNow,
                                        CreatedBy = 3 // System user ID //temp
                                    };

                                    await objRelationshipService.AddRelationshipAsync(newRelationship);
                                }
                            }
                        }
                    }
                }

                // Handle relationships not in schema (delete)
                foreach (var relationship in allRelationships)
                {
                    if (!processedRelationshipIds.Contains(relationship.RelationshipID))
                    {
                        await objRelationshipService.DeleteRelationshipAsync(relationship.RelationshipID);
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }


        public async Task<IEnumerable<SchemaRelationshipDto>> GetDatabaseRelationshipsAsync(int databaseId)
        {
            try
            {
                var result = new List<SchemaRelationshipDto>();

                // Get all tables for this database
                var tables = await objTablereposiroty.GetTablesByDatabaseIdAsync(databaseId);

                // Get all relationships for each table
                foreach (var table in tables)
                {
                    var relationships = await objRelationshipService.GetRelationshipsByTableIdAsync(table.TableID);

                    // Get table name by ID
                    var tableNameById = tables.ToDictionary(t => t.TableID, t => t.DBTableName);

                    // For each relationship, get column information and build DTOs
                    foreach (var rel in relationships)
                    {
                        // Get source and target table names
                        if (!tableNameById.TryGetValue(rel.TableID, out string sourceTableName) ||
                            !tableNameById.TryGetValue(rel.RelatedTableID, out string targetTableName))
                        {
                            continue;
                        }

                        // Get column names
                        var sourceColumn = await objColumnreposiroty.GetColumnByIdAsync(rel.ColumnID);
                        var targetColumn = await objColumnreposiroty.GetColumnByIdAsync(rel.RelatedColumnID);

                        if (sourceColumn == null || targetColumn == null)
                        {
                            continue;
                        }

                        // Create DTO
                        var relationshipDto = new SchemaRelationshipDto
                        {
                            SourceTable = sourceTableName,
                            SourceColumn = sourceColumn.DBColumnName,
                            TargetTable = targetTableName,
                            TargetColumn = targetColumn.DBColumnName,
                            RelationshipType = rel.RelationshipType,
                            Description = rel.Description
                        };

                        result.Add(relationshipDto);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                throw;
            }
        }


        /// <summary>
        /// Updates the example questions for a database.
        /// </summary>
        /// <param name="databaseId">The ID of the database.</param>
        /// <param name="exampleQuestions">The example questions to set.</param>
        /// <returns>True if the update was successful; otherwise, false.</returns>
        public async Task<bool> UpdateExampleQuestionsAsync(int databaseId, SuggestedQuestions suggestedQuestions)
        {
            try
            {
                var database = await GetDatabaseByIdAsync(databaseId);
                if (database == null)
                    return false;

                await SetExampleQuestions(database, suggestedQuestions);
                var result = await objDataBaseRepository.UpdateDatabaseAsync(database);
                return result > 0;
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Gets the example questions for a database.
        /// </summary>
        /// <param name="databaseId">The ID of the database.</param>
        /// <returns>The example questions for the database.</returns>
        public async Task<SuggestedQuestions> GetExampleQuestionsAsync(int databaseId)
        {
            try
            {
                var database = await GetDatabaseByIdAsync(databaseId);
                if (database == null)
                    return new SuggestedQuestions();

                return await GetExampleQuestions(database);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public Task<SuggestedQuestions> GetExampleQuestions(Database database)
        {
            if (string.IsNullOrEmpty(database.SuggestedQuestions))
            {
                return Task.FromResult(new SuggestedQuestions());
            }

            try
            {
                var result = System.Text.Json.JsonSerializer.Deserialize<SuggestedQuestions>(
                    database.SuggestedQuestions,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                ) ?? new SuggestedQuestions();
                return Task.FromResult(result);
            }
            catch
            {
                throw;
            }
        }

        public Task<string> SetExampleQuestions(Database database, SuggestedQuestions questions)
        {
            if (questions == null)
            {
                database.SuggestedQuestions = "{}";
                return Task.FromResult(database.SuggestedQuestions);
            }

            try
            {
                database.SuggestedQuestions = System.Text.Json.JsonSerializer.Serialize(questions);
                return Task.FromResult(database.SuggestedQuestions);
            }
            catch
            {
                throw;
            }
        }

    }
}
