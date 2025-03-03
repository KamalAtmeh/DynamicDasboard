using Dapper;
using DynamicDashboardCommon.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using DynamicDasboardWebAPI.Utilities;

namespace DynamicDasboardWebAPI.Repositories
{
    public class RelationshipRepository
    {
        private readonly IDbConnection _connection;
        private readonly DbConnectionFactory _connectionFactory;
        private readonly ILogger<QueryRepository> _logger;

        public RelationshipRepository(
                   IDbConnection connection,
                   DbConnectionFactory connectionFactory,
                   ILogger<QueryRepository> logger = null)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _logger = logger;
        }

        // Fetch relationships for a specific table
        public async Task<IEnumerable<Relationship>> GetRelationshipsByTableIdAsync(int tableId)
        {
            try
            {
                const string query = "SELECT * FROM Relationships WHERE TableID = @TableID";
                return await _connection.QuerySafeAsync<Relationship>(query, new { TableID = tableId });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving relationships for table: {TableID}", tableId);
                throw;
            }
        }

        // Get a specific relationship by ID
        public async Task<Relationship> GetRelationshipByIdAsync(int relationshipId)
        {
            try
            {
                const string query = "SELECT * FROM Relationships WHERE RelationshipID = @RelationshipID";
                return await _connection.QueryFirstOrDefaultSafeAsync<Relationship>(query, new { RelationshipID = relationshipId });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving relationship by ID: {RelationshipID}", relationshipId);
                throw;
            }
        }

        // Add a new relationship
        public async Task<int> AddRelationshipAsync(Relationship relationship)
        {
            try
            {
                const string query = @"
                    INSERT INTO Relationships (TableID, ColumnID, RelatedTableID, RelatedColumnID, 
                                          RelationshipType, Description, IsEnforced, CreatedBy)
                    VALUES (@TableID, @ColumnID, @RelatedTableID, @RelatedColumnID, 
                            @RelationshipType, @Description, @IsEnforced, @CreatedBy);
                    SELECT CAST(SCOPE_IDENTITY() as int)";

                return await _connection.ExecuteScalarSafeAsync<int>(query, relationship);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error adding relationship between tables: {TableID} and {RelatedTableID}",
                    relationship?.TableID, relationship?.RelatedTableID);
                throw;
            }
        }

        // Update an existing relationship
        public async Task<int> UpdateRelationshipAsync(Relationship relationship)
        {
            try
            {
              

                const string query = @"
                    UPDATE Relationships
                    SET TableID = @TableID, 
                        ColumnID = @ColumnID, 
                        RelatedTableID = @RelatedTableID, 
                        RelatedColumnID = @RelatedColumnID, 
                        RelationshipType = @RelationshipType, 
                        Description = @Description, 
                        IsEnforced = @IsEnforced, 
                        CreatedBy = @CreatedBy
                    WHERE RelationshipID = @RelationshipID";

                return await _connection.ExecuteSafeAsync(query, relationship);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating relationship: {RelationshipID}", relationship?.RelationshipID);
                throw;
            }
        }

        // Delete a relationship
        public async Task<int> DeleteRelationshipAsync(int relationshipId)
        {
            try
            {
                const string query = "DELETE FROM Relationships WHERE RelationshipID = @RelationshipID";
                return await _connection.ExecuteSafeAsync(query, new { RelationshipID = relationshipId });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error deleting relationship: {RelationshipID}", relationshipId);
                throw;
            }
        }
    }
}