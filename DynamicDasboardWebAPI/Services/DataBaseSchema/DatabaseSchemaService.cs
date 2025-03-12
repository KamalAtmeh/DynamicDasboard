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

namespace DynamicDasboardWebAPI.Services
{
    public class DatabaseSchemaService
    {
        private readonly DatabaseSchemaRepository _DBschemaMetadataRepository;
        private readonly DatabaseService _databaseService;

        public DatabaseSchemaService(
            DatabaseSchemaRepository repository,
            DatabaseService databaseService)
        {
            _DBschemaMetadataRepository = repository;
            _databaseService = databaseService;
        }

        #region DB Schema CRUD Operations

        public async Task<int> CreateSchemaAsync(DatabaseSchema schema)
        {
            try
            {
                return await _DBschemaMetadataRepository.InsertDatabaseJsonSchemaAsync(schema);
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
                return await _DBschemaMetadataRepository.UpdateDatabaseJsonSchemaAsync(schema);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<DatabaseSchema> GetSchemaByDataBaseIdAsync(int databaseID)
        {
            try
            {
                Database objDataBase = await _databaseService.GetDatabaseByIdAsync(databaseID);
                if (objDataBase == null || objDataBase.DatabaseID == 0)
                {
                    return null;
                }
                DatabaseSchema schema = await _DBschemaMetadataRepository.GetDatabaseJsonSchemaByIdAsync(databaseID);
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
                return await _DBschemaMetadataRepository.DeactivateDatabaseJsonSchemaAsync(databaseID);
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
                var tables = await _DBschemaMetadataRepository.GetTablesAsync(objDataBase);

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
                    var columns = await _DBschemaMetadataRepository.GetColumnsAsync(objDataBase, table.TABLE_NAME);

                    foreach (var column in columns)
                    {
                        var columnId = Guid.NewGuid().ToString();
                        columnIdMap[table.TABLE_NAME][column.COLUMN_NAME] = columnId;

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
                                Order = column.ORDINAL_POSITION
                            },
                            Constraints = new List<ConstraintSchema>()
                        });
                    }

                    schemaDetail.Tables.Add(tableSchema);
                }

                // Get relationships
                var relationships = await _DBschemaMetadataRepository.GetRelationshipsAsync(objDataBase);


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

        public async Task<DatabaseSchema> RefreshAndGetDatabaseSchemaFromConnectedDBAsync(int databaseId, Database objDataBase)
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
                var tables = await _DBschemaMetadataRepository.GetTablesAsync(objDataBase);

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
                    var columns = await _DBschemaMetadataRepository.GetColumnsAsync(objDataBase, table.TABLE_NAME);

                    foreach (var column in columns)
                    {
                        var columnId = Guid.NewGuid().ToString();
                        columnIdMap[table.TABLE_NAME][column.COLUMN_NAME] = columnId;

                        tableSchema.Columns.Add(new ColumnSchema
                        {
                            ID = columnId,
                            DBName = column.COLUMN_NAME,
                            FriendlyName = column.COLUMN_NAME, // Default to DB name
                            DataType = column.DATA_TYPE,
                            IsNullable = column.IS_NULLABLE.Equals("YES", StringComparison.OrdinalIgnoreCase),
                            IsPrimaryKey = column.IsPrimaryKey,
                            IsLookup = false,
                            Description = string.Empty,
                            Synonyms = new List<string>(),
                            UIConfig = new UiConfig
                            {
                                Visible = true,
                                Order = column.ORDINAL_POSITION
                            },
                            Constraints = new List<ConstraintSchema>()
                        });
                    }

                    schemaDetail.Tables.Add(tableSchema);
                }

                // Get relationships
                var relationships = await _DBschemaMetadataRepository.GetRelationshipsAsync(objDataBase);




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

                var existingSchema = await GetSchemaByDataBaseIdAsync(databaseId);

                if (existingSchema != null)
                {
                    existingSchema.SchemaData = schemaJson;
                    existingSchema.ModifiedAt = DateTime.UtcNow;
                    await UpdateSchemaAsync(existingSchema);
                }

                return schemaDetail;
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

        #endregion
    }
}
