using Dapper;
using DynamicDashboardCommon.Models;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace DynamicDasboardWebAPI.Repositories
{
    /// <summary>
    /// Repository class for managing relationships in the database.
    /// Provides methods to fetch, add, update, and delete relationships.
    /// </summary>
    public class RelationshipRepository
    {
        private readonly IDbConnection _connection;

        /// <summary>
        /// Initializes a new instance of the <see cref="RelationshipRepository"/> class.
        /// </summary>
        /// <param name="connection">The database connection to be used by the repository.</param>
        public RelationshipRepository(IDbConnection connection)
        {
            _connection = connection;
        }

        /// <summary>
        /// Fetches relationships for a specific table.
        /// </summary>
        /// <param name="tableId">The ID of the table for which to fetch relationships.</param>
        /// <returns>A collection of relationships associated with the specified table.</returns>
        public async Task<IEnumerable<Relationship>> GetRelationshipsByTableIdAsync(int tableId)
        {
            string query = "SELECT * FROM Relationships WHERE TableID = @TableID";
            return await _connection.QueryAsync<Relationship>(query, new { TableID = tableId });
        }

        /// <summary>
        /// Adds a new relationship to the database.
        /// </summary>
        /// <param name="relationship">The relationship object to be added.</param>
        /// <returns>The number of rows affected.</returns>
        public async Task<int> AddRelationshipAsync(Relationship relationship)
        {
            string query = @"
                    INSERT INTO Relationships (TableID, ColumnID, RelatedTableID, RelatedColumnID, 
                                              RelationshipType, Description, IsEnforced, CreatedBy)
                    VALUES (@TableID, @ColumnID, @RelatedTableID, @RelatedColumnID, 
                            @RelationshipType, @Description, @IsEnforced, @CreatedBy)";
            return await _connection.ExecuteAsync(query, relationship);
        }

        /// <summary>
        /// Updates an existing relationship in the database.
        /// </summary>
        /// <param name="relationship">The relationship object with updated values.</param>
        /// <returns>The number of rows affected.</returns>
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

        /// <summary>
        /// Deletes a relationship from the database.
        /// </summary>
        /// <param name="relationshipId">The ID of the relationship to be deleted.</param>
        /// <returns>The number of rows affected.</returns>
        public async Task<int> DeleteRelationshipAsync(int relationshipId)
        {
            string query = "DELETE FROM Relationships WHERE RelationshipID = @RelationshipID";
            return await _connection.ExecuteAsync(query, new { RelationshipID = relationshipId });
        }
    }
}