using DynamicDashboardCommon.Models;
using DynamicDasboardWebAPI.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DynamicDasboardWebAPI.Services
{
    /// <summary>  
    /// Service class for managing relationships between tables.  
    /// Provides methods to get, add, update, and delete relationships.  
    /// </summary>  
    public class RelationshipService
    {
        private readonly RelationshipRepository _repository;

        /// <summary>  
        /// Initializes a new instance of the <see cref="RelationshipService"/> class.  
        /// </summary>  
        /// <param name="repository">The repository to interact with the data source.</param>  
        public RelationshipService(RelationshipRepository repository)
        {
            _repository = repository;
        }

        /// <summary>  
        /// Gets the relationships for a specific table by its ID.  
        /// </summary>  
        /// <param name="tableId">The ID of the table.</param>  
        /// <returns>A collection of relationships associated with the specified table.</returns>  
        public async Task<IEnumerable<Relationship>> GetRelationshipsByTableIdAsync(int tableId)
        {
            return await _repository.GetRelationshipsByTableIdAsync(tableId);
        }

        /// <summary>  
        /// Adds a new relationship.  
        /// </summary>  
        /// <param name="relationship">The relationship to add.</param>  
        /// <returns>The ID of the newly added relationship.</returns>  
        /// <exception cref="ArgumentException">Thrown when the table IDs are not valid.</exception>  
        public async Task<int> AddRelationshipAsync(Relationship relationship)
        {
            if (relationship.TableID <= 0 || relationship.RelatedTableID <= 0)
                throw new ArgumentException("Table IDs are required.");

            return await _repository.AddRelationshipAsync(relationship);
        }

        /// <summary>  
        /// Updates an existing relationship.  
        /// </summary>  
        /// <param name="relationship">The relationship to update.</param>  
        /// <returns>The number of affected rows.</returns>  
        /// <exception cref="ArgumentException">Thrown when the table IDs are not valid.</exception>  
        public async Task<int> UpdateRelationshipAsync(Relationship relationship)
        {
            if (relationship.TableID <= 0 || relationship.RelatedTableID <= 0)
                throw new ArgumentException("Table IDs are required.");

            return await _repository.UpdateRelationshipAsync(relationship);
        }

        /// <summary>  
        /// Deletes a relationship by its ID.  
        /// </summary>  
        /// <param name="relationshipId">The ID of the relationship to delete.</param>  
        /// <returns>The number of affected rows.</returns>  
        public async Task<int> DeleteRelationshipAsync(int relationshipId)
        {
            return await _repository.DeleteRelationshipAsync(relationshipId);
        }
    }
}