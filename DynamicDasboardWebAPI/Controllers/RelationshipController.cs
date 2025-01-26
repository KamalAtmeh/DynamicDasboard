using DynamicDashboardCommon.Models;
using DynamicDasboardWebAPI.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DynamicDasboardWebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RelationshipsController : ControllerBase
    {
        private readonly RelationshipService _service;

        public RelationshipsController(RelationshipService service)
        {
            _service = service;
        }

        // Get relationships for a specific table
        [HttpGet("table/{tableId}")]
        public async Task<ActionResult<IEnumerable<Relationship>>> GetRelationshipsByTableId(int tableId)
        {
            var relationships = await _service.GetRelationshipsByTableIdAsync(tableId);
            return Ok(relationships);
        }

        // Add a new relationship
        [HttpPost]
        public async Task<ActionResult<int>> AddRelationship([FromBody] Relationship relationship)
        {
            var result = await _service.AddRelationshipAsync(relationship);
            return Ok(result);
        }

        // Update an existing relationship
        [HttpPut("{relationshipId}")]
        public async Task<ActionResult<int>> UpdateRelationship(int relationshipId, [FromBody] Relationship relationship)
        {
            if (relationshipId != relationship.RelationshipID)
                return BadRequest("Relationship ID mismatch.");

            var result = await _service.UpdateRelationshipAsync(relationship);
            return Ok(result);
        }

        // Delete a relationship
        [HttpDelete("{relationshipId}")]
        public async Task<ActionResult<int>> DeleteRelationship(int relationshipId)
        {
            var result = await _service.DeleteRelationshipAsync(relationshipId);
            return Ok(result);
        }
    }
}