using Dapper;
using DynamicDashboardCommon.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using DynamicDasboardWebAPI.Utilities;

namespace DynamicDasboardWebAPI.Repositories
{
    public class RelationshipRepository : BaseRepository
    {
        public RelationshipRepository(
            IDbConnection connection,
            DbConnectionFactory connectionFactory)
            : base(connection, connectionFactory)
        {
        }

        /// <summary>
        /// Fetch relationships for a specific table (default DB).
        /// </summary>
        public async Task<IEnumerable<Relationship>> GetRelationshipsByTableIdAsync(int tableId)
        {
            try
            {
                const string query = "SELECT * FROM Relationships WHERE TableID = @TableID";

                return await WithConnectionAsync(async conn =>
                {
                    return await conn.QuerySafeAsync<Relationship>(query, new { TableID = tableId });
                });
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Get a specific relationship by ID (default DB).
        /// </summary>
        public async Task<Relationship> GetRelationshipByIdAsync(int relationshipId)
        {
            try
            {
                const string query = "SELECT * FROM Relationships WHERE RelationshipID = @RelationshipID";

                return await WithConnectionAsync(async conn =>
                {
                    return await conn.QueryFirstOrDefaultSafeAsync<Relationship>(query, new { RelationshipID = relationshipId });
                });
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Add a new relationship (default DB).
        /// </summary>
        public async Task<int> AddRelationshipAsync(Relationship relationship)
        {
            try
            {
                const string query = @"
                    INSERT INTO Relationships (
                        TableID, 
                        ColumnID, 
                        RelatedTableID, 
                        RelatedColumnID, 
                        RelationshipType, 
                        Description, 
                        IsEnforced, 
                        CreatedBy
                    )
                    VALUES (
                        @TableID, 
                        @ColumnID, 
                        @RelatedTableID, 
                        @RelatedColumnID, 
                        @RelationshipType, 
                        @Description, 
                        @IsEnforced, 
                        @CreatedBy
                    );
                    SELECT CAST(SCOPE_IDENTITY() as int);";

                return await WithConnectionAsync(async conn =>
                {
                    return await conn.ExecuteScalarSafeAsync<int>(query, relationship);
                });
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Update an existing relationship (default DB).
        /// </summary>
        public async Task<int> UpdateRelationshipAsync(Relationship relationship)
        {
            try
            {
                const string query = @"
                    UPDATE Relationships
                    SET 
                        TableID = @TableID, 
                        ColumnID = @ColumnID, 
                        RelatedTableID = @RelatedTableID, 
                        RelatedColumnID = @RelatedColumnID, 
                        RelationshipType = @RelationshipType, 
                        Description = @Description, 
                        IsEnforced = @IsEnforced, 
                        CreatedBy = @CreatedBy
                    WHERE RelationshipID = @RelationshipID";

                return await WithConnectionAsync(async conn =>
                {
                    return await conn.ExecuteSafeAsync(query, relationship);
                });
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Delete a relationship (default DB).
        /// </summary>
        public async Task<int> DeleteRelationshipAsync(int relationshipId)
        {
            try
            {
                const string query = "DELETE FROM Relationships WHERE RelationshipID = @RelationshipID";

                return await WithConnectionAsync(async conn =>
                {
                    return await conn.ExecuteSafeAsync(query, new { RelationshipID = relationshipId });
                });
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
