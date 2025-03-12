using System;
using System.Threading.Tasks;
using DynamicDashboardCommon.Models;
using DynamicDasboardWebAPI.Repositories;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace DynamicDasboardWebAPI.Services
{
    public class DatabaseSchemaService
    {
        private readonly DatabaseJsonSchemaRepository _repository;

        public DatabaseSchemaService(DatabaseJsonSchemaRepository repository)
        {
            _repository = repository;
            
        }

        #region DB Schema CRUD Operations

        public async Task<int> CreateSchemaAsync(DatabaseSchema schema)
        {
            try
            {
                return await _repository.InsertDatabaseJsonSchemaAsync(schema);
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
                return await _repository.UpdateDatabaseJsonSchemaAsync(schema);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<DatabaseSchema> GetSchemaByIdAsync(int id)
        {
            try
            {
                return await _repository.GetDatabaseJsonSchemaByIdAsync(id);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<int> DeactivateSchemaAsync(int id)
        {
            try
            {
                return await _repository.DeactivateDatabaseJsonSchemaAsync(id);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        #endregion

        #region JSON Schema Operations

        // Common options for serialization/deserialization
        private  readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        /// <summary>
        /// Deserializes a JSON schema string into a DatabaseSchemaDetail object.
        /// </summary>
        public  DatabaseSchemaDetail DeserializeSchema(string jsonSchema)
        {
            if (string.IsNullOrWhiteSpace(jsonSchema))
                return new DatabaseSchemaDetail();

            try
            {
                return JsonSerializer.Deserialize<DatabaseSchemaDetail>(jsonSchema, _jsonOptions);
            }
            catch (Exception ex)
            {
                // Log the error
                throw new ArgumentException($"Failed to parse schema JSON: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Serializes a DatabaseSchemaDetail object to a JSON string.
        /// </summary>
        public  string SerializeSchema(DatabaseSchemaDetail schema)
        {
            if (schema == null)
                throw new ArgumentNullException(nameof(schema));

            return JsonSerializer.Serialize(schema, _jsonOptions);
        }

        /// <summary>
        /// Finds a table in the schema by its ID.
        /// </summary>
        public  TableSchema FindTableById(DatabaseSchemaDetail schema, int tableId)
        {
            if (schema?.Tables == null)
                return null;

            return schema.Tables.FirstOrDefault(t => t.Id.ToString() == tableId.ToString());
        }

        /// <summary>
        /// Finds a table in the schema by its database name.
        /// </summary>
        public  TableSchema FindTableByName(DatabaseSchemaDetail schema, string tableName)
        {
            if (schema?.Tables == null || string.IsNullOrWhiteSpace(tableName))
                return null;

            return schema.Tables.FirstOrDefault(t =>
                string.Equals(t.DBName, tableName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Finds a column in a table by its ID.
        /// </summary>
        public  ColumnSchema FindColumnById(TableSchema table, int columnId)
        {
            if (table?.Columns == null)
                return null;

            return table.Columns.FirstOrDefault(c => c.Id.ToString() == columnId.ToString());
        }

        /// <summary>
        /// Finds a column in a table by its database name.
        /// </summary>
        public  ColumnSchema FindColumnByName(TableSchema table, string columnName)
        {
            if (table?.Columns == null || string.IsNullOrWhiteSpace(columnName))
                return null;

            return table.Columns.FirstOrDefault(c =>
                string.Equals(c.DBName, columnName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets all relationships where the specified table is the source.
        /// </summary>
        public  List<RelationshipSchema> GetRelationshipsFromTable(DatabaseSchemaDetail schema, int tableId)
        {
            if (schema?.Relationships == null)
                return new List<RelationshipSchema>();

            return schema.Relationships
                .Where(r => r.Source.Table.ToString() == tableId.ToString())
                .ToList();
        }

        /// <summary>
        /// Gets all relationships where the specified table is the target.
        /// </summary>
        public  List<RelationshipSchema> GetRelationshipsToTable(DatabaseSchemaDetail schema, int tableId)
        {
            if (schema?.Relationships == null)
                return new List<RelationshipSchema>();

            return schema.Relationships
                .Where(r => r.Target.Table.ToString() == tableId.ToString())
                .ToList();
        }

        /// <summary>
        /// Creates a new minimal schema for a database.
        /// </summary>
        public  DatabaseSchemaDetail CreateMinimalSchema(int databaseId, string databaseName)
        {
            return new DatabaseSchemaDetail
            {
                Id = databaseId,
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
        public  void UpsertTable(DatabaseSchemaDetail schema, TableSchema table)
        {
            if (schema == null || table == null)
                return;

            if (schema.Tables == null)
                schema.Tables = new List<TableSchema>();

            var existingTable = schema.Tables.FirstOrDefault(t => t.Id.ToString() == table.Id.ToString());

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
        public  void UpsertRelationship(DatabaseSchemaDetail schema, RelationshipSchema relationship)
        {
            if (schema == null || relationship == null)
                return;

            if (schema.Relationships == null)
                schema.Relationships = new List<RelationshipSchema>();

            var existingRelationship = schema.Relationships
                .FirstOrDefault(r => r.Id.ToString() == relationship.Id.ToString());

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
        public  bool ValidateSchema(DatabaseSchemaDetail schema, out string errorMessage)
        {
            errorMessage = null;

            if (schema == null)
            {
                errorMessage = "Schema cannot be null";
                return false;
            }

            if (schema.Id <= 0)
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
                        errorMessage = $"Table with ID {table.Id} is missing a database name";
                        return false;
                    }
                }
            }

            return true;
        }

        #endregion
    }
}
