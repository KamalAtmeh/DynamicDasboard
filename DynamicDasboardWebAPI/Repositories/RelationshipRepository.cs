using Dapper;
using DynamicDashboardCommon.Models;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace DynamicDasboardWebAPI.Repositories
{
    public class RelationshipRepository
    {
        private readonly IDbConnection _connection;

        public RelationshipRepository(IDbConnection connection)
        {
            _connection = connection;
        }

        // Fetch relationships for a specific table
        public async Task<IEnumerable<Relationship>> GetRelationshipsByTableIdAsync(int tableId)
        {
            string query = "SELECT * FROM Relationships WHERE TableID = @TableID";
            return await _connection.QueryAsync<Relationship>(query, new { TableID = tableId });
        }

        // Add a new relationship
        public async Task<int> AddRelationshipAsync(Relationship relationship)
        {
            string query = @"
                INSERT INTO Relationships (TableID, ColumnID, RelatedTableID, RelatedColumnID, 
                                          RelationshipType, Description, IsEnforced, CreatedBy)
                VALUES (@TableID, @ColumnID, @RelatedTableID, @RelatedColumnID, 
                        @RelationshipType, @Description, @IsEnforced, @CreatedBy)";
            return await _connection.ExecuteAsync(query, relationship);
        }

        // Update an existing relationship
        public async Task<int> UpdateRelationshipAsync(Relationship relationship)
        {
            string query = @"
                UPDATE Relationships
                SET TableID = @TableID, ColumnID = @ColumnID, RelatedTableID = @RelatedTableID, 
                    RelatedColumnID = @RelatedColumnID, RelationshipType = @RelationshipType, 
                    Description = @Description, IsEnforced = @IsEnforced, CreatedBy = @CreatedBy
                WHERE RelationshipID = @RelationshipID";
            return await _connection.ExecuteAsync(query, relationship);
        }

        // Delete a relationship
        public async Task<int> DeleteRelationshipAsync(int relationshipId)
        {
            string query = "DELETE FROM Relationships WHERE RelationshipID = @RelationshipID";
            return await _connection.ExecuteAsync(query, new { RelationshipID = relationshipId });
        }
    }
}