using DynamicDashboardCommon.Models;
using DynamicDasboardWebAPI.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DynamicDasboardWebAPI.Services
{
    public class RelationshipService
    {
        private readonly RelationshipRepository _repository;

        public RelationshipService(RelationshipRepository repository)
        {
            _repository = repository;
        }

        // Get relationships for a specific table
        public async Task<IEnumerable<Relationship>> GetRelationshipsByTableIdAsync(int tableId)
        {
            return await _repository.GetRelationshipsByTableIdAsync(tableId);
        }

        // Add a new relationship
        public async Task<int> AddRelationshipAsync(Relationship relationship)
        {
            if (relationship.TableID <= 0 || relationship.RelatedTableID <= 0)
                throw new ArgumentException("Table IDs are required.");

            return await _repository.AddRelationshipAsync(relationship);
        }

        // Update an existing relationship
        public async Task<int> UpdateRelationshipAsync(Relationship relationship)
        {
            if (relationship.TableID <= 0 || relationship.RelatedTableID <= 0)
                throw new ArgumentException("Table IDs are required.");

            return await _repository.UpdateRelationshipAsync(relationship);
        }

        // Delete a relationship
        public async Task<int> DeleteRelationshipAsync(int relationshipId)
        {
            return await _repository.DeleteRelationshipAsync(relationshipId);
        }
    }
}