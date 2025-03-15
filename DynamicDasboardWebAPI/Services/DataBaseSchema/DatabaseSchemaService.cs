using System;
using System.Threading.Tasks;
using DynamicDashboardCommon.Models;
using DynamicDasboardWebAPI.Repositories;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using DynamicDashboardCommon.Enums;
using System.Data;
using DynamicDasboardWebAPI.Utilities;
using System.Linq.Expressions;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.HttpResults;
using System.ComponentModel;
using DynamicDashboardCommon.Helper;

namespace DynamicDasboardWebAPI.Services
{
    public class DatabaseSchemaService
    {
        private readonly DatabaseSchemaRepository objDBschemaMetadataRepository;
        private readonly DatabaseService objDataDaseService;

        public DatabaseSchemaService(
            DatabaseSchemaRepository repository,
            DatabaseService databaseService)
        {
            objDBschemaMetadataRepository = repository;
            objDataDaseService = databaseService;
        }

        #region DB Schema CRUD Operations

        public async Task<int> CreateSchemaAsync(DatabaseSchema schema)
        {
            try
            {
                return await objDBschemaMetadataRepository.InsertDatabaseJsonSchemaAsync(schema);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<int> UpdateSchemaAsync(DatabaseSchema schema)
        {
            try
            {
                return await objDBschemaMetadataRepository.UpdateDatabaseJsonSchemaAsync(schema);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<DatabaseSchema> GetJsonSchemaByDataBaseIdAsync(int databaseID)
        {
            try
            {
                Database objDataBase = await objDataDaseService.GetDatabaseByIdAsync(databaseID);
                if (objDataBase == null || objDataBase.DatabaseID == 0)
                {
                    return null;
                }
                DatabaseSchema schema = await objDBschemaMetadataRepository.GetDatabaseJsonSchemaByIdAsync(databaseID);
                if ((schema == null || schema.ID == 0 || string.IsNullOrEmpty(schema.SchemaData)) && databaseID > 0)
                {
                    return await GenerateAndGetDatabaseSchemaFromConnectedDBAsync(databaseID, objDataBase);
                }
                else
                {
                    return schema;
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<int> DeactivateSchemaAsync(int databaseID)
        {
            try
            {
                return await objDBschemaMetadataRepository.DeactivateDatabaseJsonSchemaAsync(databaseID);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        /// <summary>
        /// Retrieves the database schema from a connected database and saves it in our schema format.
        /// </summary>
        public async Task<DatabaseSchema> GenerateAndGetDatabaseSchemaFromConnectedDBAsync(int databaseId, Database objDataBase)
        {
            try
            {

                // Get database connection details

                if (objDataBase == null)
                    throw new ArgumentException($"Database with ID {databaseId} not found");

                // Create schema structure
                var schemaDetail = new DatabaseSchema
                {
                    ID = databaseId,
                    Name = objDataBase.Name,
                    Version = new VersionInfo
                    {
                        Number = "1.0.0", //temp
                        Description = string.Empty,
                        Created = DateTime.UtcNow,
                        Modified = DateTime.UtcNow
                    },
                    Tables = new List<TableSchema>(),
                    Relationships = new List<RelationshipSchema>()
                };

                // Get tables
                var tables = await objDBschemaMetadataRepository.GetTablesAsync(objDataBase);

                // Create ID mapping dictionaries
                var tableIdMap = new Dictionary<string, string>();
                var columnIdMap = new Dictionary<string, Dictionary<string, string>>();

                // Process tables and columns
                foreach (var table in tables)
                {
                    var tableId = Guid.NewGuid().ToString();
                    tableIdMap[table.TABLE_NAME] = tableId;
                    columnIdMap[table.TABLE_NAME] = new Dictionary<string, string>();

                    var tableSchema = new TableSchema
                    {
                        ID = tableId,
                        Status = EnumDataBaseStatus.Active.ToString(),
                        DBName = table.TABLE_NAME,
                        FriendlyName = table.TABLE_NAME, // Default to DB name
                        Description = string.Empty,
                        Columns = new List<ColumnSchema>(),
                        Synonyms = new List<string>()
                    };

                    // Get columns for this table
                    var columns = await objDBschemaMetadataRepository.GetColumnsAsync(objDataBase, table.TABLE_NAME);
                    tableSchema.TotalColumns = columns.Count;

                    foreach (var column in columns)
                    {
                        var columnId = Guid.NewGuid().ToString();
                        columnIdMap[table.TABLE_NAME][column.COLUMN_NAME] = columnId;
                        int order = 0;
                        if (column.ORDINAL_POSITION != null)
                        {
                            int.TryParse( Convert.ToString(column.ORDINAL_POSITION), out order);
                        }
                        tableSchema.Columns.Add(new ColumnSchema
                        {
                            ID = columnId,
                            DBName = column.COLUMN_NAME,
                            FriendlyName = column.COLUMN_NAME, // Default to DB name
                            DataType = column.DATA_TYPE,
                            IsNullable = column.IS_NULLABLE.Equals("YES", StringComparison.OrdinalIgnoreCase),
                            IsPrimaryKey = (column.IsPrimaryKeyInt == 1),
                            IsLookup = false,
                            Description = string.Empty,
                            Synonyms = new List<string>(),
                            UIConfig = new UiConfig
                            {
                                Visible = true,
                                Order = order
                            },
                            Constraints = new List<ConstraintSchema>()
                        });
                    }

                    schemaDetail.Tables.Add(tableSchema);
                }

                // Get relationships
                var relationships = await objDBschemaMetadataRepository.GetRelationshipsAsync(objDataBase);


                foreach (var rel in relationships)
                {
                    string sourceTableId = string.Empty, targetTableId = string.Empty, sourceColumnId = string.Empty, targetColumnId = string.Empty;

                    if (!tableIdMap.TryGetValue(rel.FK_TABLE, out sourceTableId) ||
                        !tableIdMap.TryGetValue(rel.PK_TABLE, out targetTableId) ||
                        !columnIdMap[rel.FK_TABLE].TryGetValue(rel.FK_COLUMN, out sourceColumnId) ||
                        !columnIdMap[rel.PK_TABLE].TryGetValue(rel.PK_COLUMN, out targetColumnId))
                    {
                        continue; // Skip if we can't find the tables/columns
                    }

                    // Get the source table and column objects
                    var sourceTable = schemaDetail.Tables.FirstOrDefault(t => t.ID == sourceTableId);
                    var sourceColumn = sourceTable?.Columns?.FirstOrDefault(c => c.ID == sourceColumnId);

                    // Get the target table and column objects
                    var targetTable = schemaDetail.Tables.FirstOrDefault(t => t.ID == targetTableId);
                    var targetColumn = targetTable?.Columns?.FirstOrDefault(c => c.ID == targetColumnId);

                    // Skip if any object is missing
                    if (sourceTable == null || sourceColumn == null || targetTable == null || targetColumn == null)
                    {
                        continue;
                    }

                    schemaDetail.Relationships.Add(new RelationshipSchema
                    {
                        ID = Guid.NewGuid().ToString(),
                        Name = $"FK_{rel.FK_TABLE}_{rel.FK_COLUMN}_TO_{rel.PK_TABLE}_{rel.PK_COLUMN}",
                        Type = EnumRelationShipType.OneToMany.ToString(),
                        Status = EnumDataBaseStatus.Active.ToString(),
                        Source = new RelationshipDetails
                        {
                            TableID = sourceTableId,
                            TableName = !string.IsNullOrEmpty(sourceTable.FriendlyName) ? sourceTable.FriendlyName : sourceTable.DBName,
                            ColumnID = sourceColumnId,
                            ColumnName = !string.IsNullOrEmpty(sourceColumn.FriendlyName) ? sourceColumn.FriendlyName : sourceColumn.DBName
                        },
                        Target = new RelationshipDetails
                        {
                            TableID = targetTableId,
                            TableName = !string.IsNullOrEmpty(targetTable.FriendlyName) ? targetTable.FriendlyName : targetTable.DBName,
                            ColumnID = targetColumnId,
                            ColumnName = !string.IsNullOrEmpty(targetColumn.FriendlyName) ? targetColumn.FriendlyName : targetColumn.DBName
                        },
                        Enforced = true,
                        Metadata = new RelationshipMetadata
                        {
                            Confidence = 1.0,
                            DiscoveredAt = DateTime.UtcNow,
                            LastValidated = DateTime.UtcNow
                        }
                    });
                }

                // Serialize the schema
                string schemaJson = SerializeSchema(schemaDetail);

                // Create new schema entry or update existing

                var newSchema = new DatabaseSchema
                {
                    DataBaseID = databaseId,
                    Name = objDataBase.Name,
                    Status = (int)EnumDataBaseStatus.Active,
                    SchemaData = schemaJson,
                    CreatedAt = DateTime.UtcNow,
                    ModifiedAt = DateTime.UtcNow
                };

                await CreateSchemaAsync(newSchema);
                return schemaDetail;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        /// <summary>
        /// Refreshes the database schema while preserving user-defined metadata
        /// </summary>
        public async Task<DatabaseSchema> RefreshAndGetDatabaseSchemaFromConnectedDBAsync(int databaseId, Database objDataBase)
        {
            try
            {
                if (objDataBase == null)
                    throw new ArgumentException($"Database with ID {databaseId} not found");

                // Get existing schema if available
                var existingSchema = await GetJsonSchemaByDataBaseIdAsync(databaseId);
                bool hasExistingSchema = existingSchema != null && !string.IsNullOrEmpty(existingSchema.SchemaData);

                DatabaseSchema existingSchemaObj = null;
                if (hasExistingSchema)
                {
                    existingSchemaObj = DeserializeSchema(existingSchema.SchemaData);
                }

                // Create new schema structure
                var newSchemaObj = new DatabaseSchema
                {
                    ID = databaseId,
                    Name = objDataBase.Name,
                    Version = new VersionInfo
                    {
                        Number = existingSchemaObj?.Version?.Number ?? "1.0.0",
                        Description = existingSchemaObj?.Version?.Description ?? string.Empty,
                        Created = existingSchemaObj?.Version?.Created ?? DateTime.UtcNow,
                        Modified = DateTime.UtcNow
                    },
                    Tables = new List<TableSchema>(),
                    Relationships = new List<RelationshipSchema>()
                };

                // Get tables
                var tables = await objDBschemaMetadataRepository.GetTablesAsync(objDataBase);

                // Create ID mapping dictionaries
                var tableIdMap = new Dictionary<string, string>();
                var columnIdMap = new Dictionary<string, Dictionary<string, string>>();

                // Process tables and columns
                foreach (var table in tables)
                {
                    var tableId = Guid.NewGuid().ToString();
                    tableIdMap[table.TABLE_NAME] = tableId;
                    columnIdMap[table.TABLE_NAME] = new Dictionary<string, string>();

                    // Look for existing table metadata
                    TableSchema existingTable = null;
                    if (existingSchemaObj?.Tables != null)
                    {
                        existingTable = existingSchemaObj.Tables.FirstOrDefault(t =>
                            string.Equals(t.DBName, table.TABLE_NAME, StringComparison.OrdinalIgnoreCase));
                    }

                    var tableSchema = new TableSchema
                    {
                        ID = existingTable?.ID ?? tableId,
                        Status = existingTable?.Status ?? EnumDataBaseStatus.Active.ToString(),
                        DBName = table.TABLE_NAME,
                        FriendlyName = existingTable?.FriendlyName ?? table.TABLE_NAME, // Preserve friendly name
                        Description = existingTable?.Description ?? string.Empty, // Preserve description
                        IsActive = existingTable?.IsActive ?? true, // Preserve active state
                        Columns = new List<ColumnSchema>(),
                        Synonyms = existingTable?.Synonyms ?? new List<string>()
                    };

                    // Get columns for this table
                    var columns = await objDBschemaMetadataRepository.GetColumnsAsync(objDataBase, table.TABLE_NAME);
                    tableSchema.TotalColumns = columns.Count;

                    foreach (var column in columns)
                    {
                        var columnId = Guid.NewGuid().ToString();
                        columnIdMap[table.TABLE_NAME][column.COLUMN_NAME] = columnId;

                        // Look for existing column metadata
                        ColumnSchema existingColumn = null;
                        if (existingTable?.Columns != null)
                        {
                            existingColumn = existingTable.Columns.FirstOrDefault(c =>
                                string.Equals(c.DBName, column.COLUMN_NAME, StringComparison.OrdinalIgnoreCase));
                        }

                        tableSchema.Columns.Add(new ColumnSchema
                        {
                            ID = existingColumn?.ID ?? columnId,
                            DBName = column.COLUMN_NAME,
                            FriendlyName = existingColumn?.FriendlyName ?? column.COLUMN_NAME, // Preserve friendly name
                            DataType = column.DATA_TYPE,
                            IsNullable = column.IS_NULLABLE.Equals("YES", StringComparison.OrdinalIgnoreCase),
                            IsPrimaryKey = column.IsPrimaryKey == 1,
                            IsLookup = existingColumn?.IsLookup ?? false, // Preserve lookup flag
                            Description = existingColumn?.Description ?? string.Empty, // Preserve description
                            IsActive = existingColumn?.IsActive ?? true, // Preserve active state
                            Synonyms = existingColumn?.Synonyms ?? new List<string>(),
                            UIConfig = existingColumn?.UIConfig ?? new UiConfig
                            {
                                Visible = true,
                                Order = int.TryParse(column.ORDINAL_POSITION, out int order) ? order : 0
                            },
                            Constraints = existingColumn?.Constraints ?? new List<ConstraintSchema>()
                        });
                    }

                    newSchemaObj.Tables.Add(tableSchema);
                }

                // Get relationships
                var relationships = await objDBschemaMetadataRepository.GetRelationshipsAsync(objDataBase);

                foreach (var rel in relationships)
                {
                    string sourceTableId = string.Empty, targetTableId = string.Empty, sourceColumnId = string.Empty, targetColumnId = string.Empty;

                    if (!tableIdMap.TryGetValue(rel.FK_TABLE, out sourceTableId) ||
                        !tableIdMap.TryGetValue(rel.PK_TABLE, out targetTableId) ||
                        !columnIdMap[rel.FK_TABLE].TryGetValue(rel.FK_COLUMN, out sourceColumnId) ||
                        !columnIdMap[rel.PK_TABLE].TryGetValue(rel.PK_COLUMN, out targetColumnId))
                    {
                        continue; // Skip if we can't find the tables/columns
                    }

                    // Get the source table and column objects
                    var sourceTable = newSchemaObj.Tables.FirstOrDefault(t => t.ID == sourceTableId);
                    var sourceColumn = sourceTable?.Columns?.FirstOrDefault(c => c.ID == sourceColumnId);

                    // Get the target table and column objects
                    var targetTable = newSchemaObj.Tables.FirstOrDefault(t => t.ID == targetTableId);
                    var targetColumn = targetTable?.Columns?.FirstOrDefault(c => c.ID == targetColumnId);

                    // Skip if any object is missing
                    if (sourceTable == null || sourceColumn == null || targetTable == null || targetColumn == null)
                    {
                        continue;
                    }

                    // Look for existing relationship metadata
                    RelationshipSchema existingRelationship = null;
                    if (existingSchemaObj?.Relationships != null)
                    {
                        existingRelationship = existingSchemaObj.Relationships.FirstOrDefault(r =>
                            string.Equals(r.Source?.TableName, sourceTable.DBName, StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(r.Source?.ColumnName, sourceColumn.DBName, StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(r.Target?.TableName, targetTable.DBName, StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(r.Target?.ColumnName, targetColumn.DBName, StringComparison.OrdinalIgnoreCase));
                    }

                    var relationshipId = Guid.NewGuid().ToString();
                    newSchemaObj.Relationships.Add(new RelationshipSchema
                    {
                        ID = existingRelationship?.ID ?? relationshipId,
                        Name = existingRelationship?.Name ?? $"FK_{rel.FK_TABLE}_{rel.FK_COLUMN}_TO_{rel.PK_TABLE}_{rel.PK_COLUMN}",
                        Type = existingRelationship?.Type ?? EnumRelationShipType.OneToMany.ToString(),
                        Status = existingRelationship?.Status ?? EnumDataBaseStatus.Active.ToString(),
                        IsActive = existingRelationship?.IsActive ?? true, // Preserve active state
                        Source = new RelationshipDetails
                        {
                            TableID = sourceTableId,
                            TableName = !string.IsNullOrEmpty(sourceTable.FriendlyName) ? sourceTable.FriendlyName : sourceTable.DBName,
                            ColumnID = sourceColumnId,
                            ColumnName = !string.IsNullOrEmpty(sourceColumn.FriendlyName) ? sourceColumn.FriendlyName : sourceColumn.DBName
                        },
                        Target = new RelationshipDetails
                        {
                            TableID = targetTableId,
                            TableName = !string.IsNullOrEmpty(targetTable.FriendlyName) ? targetTable.FriendlyName : targetTable.DBName,
                            ColumnID = targetColumnId,
                            ColumnName = !string.IsNullOrEmpty(targetColumn.FriendlyName) ? targetColumn.FriendlyName : targetColumn.DBName
                        },
                        Enforced = existingRelationship?.Enforced ?? true,
                        Metadata = existingRelationship?.Metadata ?? new RelationshipMetadata
                        {
                            Confidence = 1.0,
                            DiscoveredAt = DateTime.UtcNow,
                            LastValidated = DateTime.UtcNow
                        }
                    });
                }

                // Preserve analysis results if they exist
                if (existingSchemaObj?.AnalysisResults != null)
                {
                    newSchemaObj.AnalysisResults = existingSchemaObj.AnalysisResults;
                }

                // Serialize the schema
                string schemaJson = SerializeSchema(newSchemaObj);

                // Update schema in database
                if (existingSchema != null)
                {
                    existingSchema.SchemaData = schemaJson;
                    existingSchema.ModifiedAt = DateTime.UtcNow;
                    await UpdateSchemaAsync(existingSchema);
                }
                else
                {
                    var newSchema = new DatabaseSchema
                    {
                        DataBaseID = databaseId,
                        Name = objDataBase.Name,
                        Status = (int)EnumDataBaseStatus.Active,
                        SchemaData = schemaJson,
                        CreatedAt = DateTime.UtcNow,
                        ModifiedAt = DateTime.UtcNow
                    };
                    await CreateSchemaAsync(newSchema);
                }

                return newSchemaObj;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        /// <summary>
        /// Updates the active status of a table
        /// </summary>
        public async Task<bool> UpdateTableActiveStatusAsync(int databaseId, string tableId, bool isActive)
        {
            try
            {
                var schema = await GetJsonSchemaByDataBaseIdAsync(databaseId);
                if (schema == null)
                    return false;

                var schemaObj = DeserializeSchema(schema.SchemaData);
                if (schemaObj == null || schemaObj.Tables == null)
                    return false;

                var table = schemaObj.Tables.FirstOrDefault(t => t.ID == tableId);
                if (table == null)
                    return false;

                table.IsActive = isActive;

                // Update schema in database
                schema.SchemaData = SerializeSchema(schemaObj);
                schema.ModifiedAt = DateTime.UtcNow;
                await UpdateSchemaAsync(schema);

                return true;
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Updates the active status of a column
        /// </summary>
        public async Task<bool> UpdateColumnActiveStatusAsync(int databaseId, string tableId, string columnId, bool isActive)
        {
            try
            {
                var schema = await GetJsonSchemaByDataBaseIdAsync(databaseId);
                if (schema == null)
                    return false;

                var schemaObj = DeserializeSchema(schema.SchemaData);
                if (schemaObj == null || schemaObj.Tables == null)
                    return false;

                var table = schemaObj.Tables.FirstOrDefault(t => t.ID == tableId);
                if (table == null || table.Columns == null)
                    return false;

                var column = table.Columns.FirstOrDefault(c => c.ID == columnId);
                if (column == null)
                    return false;

                column.IsActive = isActive;

                // Update schema in database
                schema.SchemaData = SerializeSchema(schemaObj);
                schema.ModifiedAt = DateTime.UtcNow;
                await UpdateSchemaAsync(schema);

                return true;
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Updates the active status of a relationship
        /// </summary>
        public async Task<bool> UpdateRelationshipActiveStatusAsync(int databaseId, string relationshipId, bool isActive)
        {
            try
            {
                var schema = await GetJsonSchemaByDataBaseIdAsync(databaseId);
                if (schema == null)
                    return false;

                var schemaObj = DeserializeSchema(schema.SchemaData);
                if (schemaObj == null || schemaObj.Relationships == null)
                    return false;

                var relationship = schemaObj.Relationships.FirstOrDefault(r => r.ID == relationshipId);
                if (relationship == null)
                    return false;

                relationship.IsActive = isActive;

                // Update schema in database
                schema.SchemaData = SerializeSchema(schemaObj);
                schema.ModifiedAt = DateTime.UtcNow;
                await UpdateSchemaAsync(schema);

                return true;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<bool> UpdateSchemaTable(int databaseId, string tableId, [FromBody] TableSchema tableUpdate)
        {
            try
            {
                // Get existing schema
                var schema = await GetJsonSchemaByDataBaseIdAsync(databaseId);
                if (schema == null)
                    return false;

                // Parse schema to object
                var schemaObj = DeserializeSchema(schema.SchemaData);

                // Find and update only the specific table
                var table = schemaObj.Tables.FirstOrDefault(t => t.ID == tableId);
                if (table == null)
                    return false;

                // Update only the fields sent from client
                table.FriendlyName = tableUpdate.FriendlyName;
                table.Description = tableUpdate.Description;
                table.Synonyms = tableUpdate.Synonyms;
                table.IsActive = tableUpdate.IsActive;

                // Serialize and save
                schema.SchemaData = SerializeSchema(schemaObj);
                await UpdateSchemaAsync(schema);

                return true;

            }
            catch (Exception ex)
            {
                throw;
            }
        }

        #endregion

        #region JSON Schema Operations

        // Common options for serialization/deserialization
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        /// <summary>
        /// Deserializes a JSON schema string into a DatabaseSchema object.
        /// </summary>
        public DatabaseSchema DeserializeSchema(string jsonSchema)
        {
            if (string.IsNullOrWhiteSpace(jsonSchema))
                return new DatabaseSchema();

            try
            {
                return JsonSerializer.Deserialize<DatabaseSchema>(jsonSchema, _jsonOptions);
            }
            catch (Exception ex)
            {
                // Log the error
                throw;
            }
        }

        /// <summary>
        /// Serializes a DatabaseSchema object to a JSON string.
        /// </summary>
        public string SerializeSchema(DatabaseSchema schema)
        {
            try
            {
                if (schema == null)
                    throw new ArgumentNullException(nameof(schema));

                return JsonSerializer.Serialize(schema, _jsonOptions);
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Finds a table in the schema by its ID.
        /// </summary>
        public TableSchema FindTableById(DatabaseSchema schema, int tableId)
        {
            try
            {
                if (schema?.Tables == null)
                    return null;

                return schema.Tables.FirstOrDefault(t => t.ID.ToString() == tableId.ToString());
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Finds a table in the schema by its database name.
        /// </summary>
        public TableSchema FindTableByName(DatabaseSchema schema, string tableName)
        {
            if (schema?.Tables == null || string.IsNullOrWhiteSpace(tableName))
                return null;

            return schema.Tables.FirstOrDefault(t =>
                string.Equals(t.DBName, tableName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Finds a column in a table by its ID.
        /// </summary>
        public ColumnSchema FindColumnById(TableSchema table, int columnId)
        {
            if (table?.Columns == null)
                return null;

            return table.Columns.FirstOrDefault(c => c.ID.ToString() == columnId.ToString());
        }

        /// <summary>
        /// Finds a column in a table by its database name.
        /// </summary>
        public ColumnSchema FindColumnByName(TableSchema table, string columnName)
        {
            if (table?.Columns == null || string.IsNullOrWhiteSpace(columnName))
                return null;

            return table.Columns.FirstOrDefault(c =>
                string.Equals(c.DBName, columnName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets all relationships where the specified table is the source.
        /// </summary>
        public List<RelationshipSchema> GetRelationshipsFromTable(DatabaseSchema schema, int tableId)
        {
            if (schema?.Relationships == null)
                return new List<RelationshipSchema>();

            return schema.Relationships
                .Where(r => r.Source.TableID.ToString() == tableId.ToString())
                .ToList();
        }

        /// <summary>
        /// Gets all relationships where the specified table is the target.
        /// </summary>
        public List<RelationshipSchema> GetRelationshipsToTable(DatabaseSchema schema, int tableId)
        {
            if (schema?.Relationships == null)
                return new List<RelationshipSchema>();

            return schema.Relationships
                .Where(r => r.Target.TableID.ToString() == tableId.ToString())
                .ToList();
        }

        /// <summary>
        /// Creates a new minimal schema for a database.
        /// </summary>
        public DatabaseSchema CreateMinimalSchema(int databaseId, string databaseName)
        {
            return new DatabaseSchema
            {
                ID = databaseId,
                Name = databaseName,
                Tables = new List<TableSchema>(),
                Relationships = new List<RelationshipSchema>(),
                Version = new VersionInfo
                {
                    Number = "1.0.0",
                    Description = "Initial schema",
                    Created = DateTime.UtcNow,
                    Modified = DateTime.UtcNow
                }
            };
        }

        /// <summary>
        /// Updates a table in the schema, adding it if it doesn't exist.
        /// </summary>
        public void UpsertTable(DatabaseSchema schema, TableSchema table)
        {
            if (schema == null || table == null)
                return;

            if (schema.Tables == null)
                schema.Tables = new List<TableSchema>();

            var existingTable = schema.Tables.FirstOrDefault(t => t.ID.ToString() == table.ID.ToString());

            if (existingTable != null)
            {
                // Update existing table
                var index = schema.Tables.IndexOf(existingTable);
                schema.Tables[index] = table;
            }
            else
            {
                // Add new table
                schema.Tables.Add(table);
            }
        }

        /// <summary>
        /// Updates a relationship in the schema, adding it if it doesn't exist.
        /// </summary>
        public void UpsertRelationship(DatabaseSchema schema, RelationshipSchema relationship)
        {
            if (schema == null || relationship == null)
                return;

            if (schema.Relationships == null)
                schema.Relationships = new List<RelationshipSchema>();

            var existingRelationship = schema.Relationships
                .FirstOrDefault(r => r.ID.ToString() == relationship.ID.ToString());

            if (existingRelationship != null)
            {
                // Update existing relationship
                var index = schema.Relationships.IndexOf(existingRelationship);
                schema.Relationships[index] = relationship;
            }
            else
            {
                // Add new relationship
                schema.Relationships.Add(relationship);
            }
        }

        /// <summary>
        /// Validates that the schema structure is correct.
        /// </summary>
        public bool ValidateSchema(DatabaseSchema schema, out string errorMessage)
        {
            errorMessage = null;

            if (schema == null)
            {
                errorMessage = "Schema cannot be null";
                return false;
            }

            if (schema.ID <= 0)
            {
                errorMessage = "Schema must have a valid database ID";
                return false;
            }

            if (string.IsNullOrWhiteSpace(schema.Name))
            {
                errorMessage = "Schema must have a database name";
                return false;
            }

            // Validate tables if present
            if (schema.Tables != null)
            {
                foreach (var table in schema.Tables)
                {
                    if (string.IsNullOrWhiteSpace(table.DBName))
                    {
                        errorMessage = $"Table with ID {table.ID} is missing a database name";
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Optimizes the schema JSON by extracting only the essential elements
        /// </summary>
        /// <param name="schemaJson">The full schema JSON</param>
        /// <returns>Optimized schema string</returns>
        public string OptimizeSchemaForLlm(string schemaJson)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(schemaJson))
                    return string.Empty;

                // Deserialize the schema
                var schema = JsonSerializer.Deserialize<DatabaseSchema>(schemaJson);
                if (schema == null)
                    return string.Empty;

                return BuildOptimizedSchemaString(schema);
            }
            catch (Exception ex)
            {
                // Log error
                return string.Empty;
            }
        }

        /// <summary>
        /// Builds an optimized schema string from a DatabaseSchema object
        /// </summary>
        /// <param name="schema">The DatabaseSchema object, expecting it desarilized</param>
        /// <returns>Optimized schema string</returns>
        public string BuildOptimizedSchemaString(DatabaseSchema schema)
        {
            if (schema == null || schema.Tables == null || !schema.Tables.Any())
                return string.Empty;

            var result = new StringBuilder();
            result.AppendLine("Tables:");

            // Add tables and columns
            foreach (var table in schema.Tables)
            {
                if (table == null || string.IsNullOrWhiteSpace(table.DBName))
                    continue;

                result.AppendLine($"- {table.DBName}");
                if (!string.IsNullOrWhiteSpace(table.FriendlyName) && table.FriendlyName != table.DBName)
                    result.AppendLine($"  (Friendly name: {table.FriendlyName})");

                if (!string.IsNullOrWhiteSpace(table.Description))
                    result.AppendLine($"  Description: {table.Description}");

                if (table.Columns != null && table.Columns.Any())
                {
                    result.AppendLine("  Columns:");
                    foreach (var column in table.Columns)
                    {
                        if (column == null || string.IsNullOrWhiteSpace(column.DBName))
                            continue;

                        var columnDesc = $"    - {column.DBName} ({column.DataType})";
                        if (column.IsPrimaryKey)
                            columnDesc += " (Primary Key)";
                        if (column.IsLookup)
                            columnDesc += " (Lookup)";
                        if (!column.IsNullable)
                            columnDesc += " (Not Null)";

                        result.AppendLine(columnDesc);

                        if (!string.IsNullOrWhiteSpace(column.FriendlyName) && column.FriendlyName != column.DBName)
                            result.AppendLine($"      Friendly name: {column.FriendlyName}");

                        if (!string.IsNullOrWhiteSpace(column.Description))
                            result.AppendLine($"      Description: {column.Description}");
                    }
                }
            }

            // Add relationships
            if (schema.Relationships != null && schema.Relationships.Any())
            {
                result.AppendLine("\nRelationships:");
                foreach (var relationship in schema.Relationships)
                {
                    if (relationship == null || relationship.Source == null || relationship.Target == null)
                        continue;

                    result.AppendLine($"- {relationship.Source.TableName}.{relationship.Source.ColumnName} -> " +
                                     $"{relationship.Target.TableName}.{relationship.Target.ColumnName} " +
                                     $"({relationship.Type})");
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// Extracts admin descriptions from a schema for friendly terminology
        /// </summary>
        /// <param name="schema">The DatabaseSchema object</param>
        /// <returns>Dictionary mapping technical terms to friendly terms</returns>
        public Dictionary<string, string> ExtractAdminDescriptions(DatabaseSchema schema)
        {
            var descriptions = new Dictionary<string, string>();

            if (schema == null || schema.Tables == null)
                return descriptions;

            foreach (var table in schema.Tables)
            {
                if (table == null || string.IsNullOrWhiteSpace(table.DBName))
                    continue;

                // Add table friendly name if available
                if (!string.IsNullOrWhiteSpace(table.FriendlyName) && table.FriendlyName != table.DBName)
                    descriptions[table.DBName] = table.FriendlyName;

                // Add table description if available
                if (!string.IsNullOrWhiteSpace(table.Description))
                    descriptions[$"{table.DBName} description"] = table.Description;

                // Add column friendly names and descriptions
                if (table.Columns != null)
                {
                    foreach (var column in table.Columns)
                    {
                        if (column == null || string.IsNullOrWhiteSpace(column.DBName))
                            continue;

                        // Add column friendly name if available
                        if (!string.IsNullOrWhiteSpace(column.FriendlyName) && column.FriendlyName != column.DBName)
                            descriptions[$"{table.DBName}.{column.DBName}"] = column.FriendlyName;

                        // Add column description if available
                        if (!string.IsNullOrWhiteSpace(column.Description))
                            descriptions[$"{table.DBName}.{column.DBName} description"] = column.Description;
                    }
                }
            }

            return descriptions;
        }

        public async Task<List<TableSchema>> GetSchemaBasicTablesList(int databaseID)
        {
            try
            {
                DatabaseSchema objDBSchema = await GetSchemaDeserialized(databaseID);

                if (objDBSchema == null || objDBSchema.ID == 0)
                {
                    return null;
                }

                List<TableSchema> lstSchemaTables = new List<TableSchema>();

                foreach (var table in objDBSchema.Tables)
                {
                    TableSchema objTable = new TableSchema
                    {
                        ID = table.ID,
                        FriendlyName = table.FriendlyName,
                        TotalColumns = table.TotalColumns
                    };
                    lstSchemaTables.Add(objTable);
                }

                return lstSchemaTables;


            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<TableSchema> GetSchemaTableDetailsByID(int databaseID, string tableID)
        {
            try
            {
                DatabaseSchema objDBSchema = await GetSchemaDeserialized(databaseID);
                if (objDBSchema == null || objDBSchema.ID == 0)
                {
                    return null;
                }
                TableSchema objTable = objDBSchema.Tables.FirstOrDefault(t => t.ID == tableID);
                return objTable;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<DatabaseSchema> GetSchemaDeserialized(int databaseID)
        {
            try
            {
                var cacheKey = $"DatabaseSchema_{databaseID}";
                var cached = CacheHelper.Get<DatabaseSchema>(cacheKey);
                if (cached != null)
                {
                    return cached;
                }

                var schema = await GetJsonSchemaByDataBaseIdAsync(databaseID);
                if (schema == null)
                    return null;

                DatabaseSchema objSchemaDetail = DeserializeSchema(schema.SchemaData);

                await CacheHelper.AddOrUpdateAsync(cacheKey, objSchemaDetail);

                return schema;
            }
            catch (Exception ex)
            {
                throw;
            }


            #endregion
        }
    }
}
