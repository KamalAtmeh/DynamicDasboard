using DynamicDashboardCommon.Models;
using DynamicDasboardWebAPI.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DynamicDasboardWebAPI.Controllers
{
    /// <summary>
    /// API Controller for managing relationships between tables.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class RelationshipsController : ControllerBase
    {
        private readonly RelationshipService _service;

        /// <summary>
        /// Initializes a new instance of the <see cref="RelationshipsController"/> class.
        /// </summary>
        /// <param name="service">The relationship service.</param>
        public RelationshipsController(RelationshipService service)
        {
            _service = service;
        }

        /// <summary>
        /// Gets the relationships for a specific table.
        /// </summary>
        /// <param name="tableId">The ID of the table.</param>
        /// <returns>A list of relationships for the specified table.</returns>
        [HttpGet("table/{tableId}")]
        public async Task<ActionResult<IEnumerable<Relationship>>> GetRelationshipsByTableId(int tableId)
        {
            var relationships = await _service.GetRelationshipsByTableIdAsync(tableId);
            return Ok(relationships);
        }

        /// <summary>
        /// Adds a new relationship.
        /// </summary>
        /// <param name="relationship">The relationship to add.</param>
        /// <returns>The ID of the newly created relationship.</returns>
        [HttpPost]
        public async Task<ActionResult<int>> AddRelationship([FromBody] Relationship relationship)
        {
            var result = await _service.AddRelationshipAsync(relationship);
            return Ok(result);
        }

        /// <summary>
        /// Updates an existing relationship.
        /// </summary>
        /// <param name="relationshipId">The ID of the relationship to update.</param>
        /// <param name="relationship">The updated relationship data.</param>
        /// <returns>The ID of the updated relationship.</returns>
        [HttpPut("{relationshipId}")]
        public async Task<ActionResult<int>> UpdateRelationship(int relationshipId, [FromBody] Relationship relationship)
        {
            if (relationshipId != relationship.RelationshipID)
                return BadRequest("Relationship ID mismatch.");

            var result = await _service.UpdateRelationshipAsync(relationship);
            return Ok(result);
        }

        /// <summary>
        /// Deletes a relationship.
        /// </summary>
        /// <param name="relationshipId">The ID of the relationship to delete.</param>
        /// <returns>The ID of the deleted relationship.</returns>
        [HttpDelete("{relationshipId}")]
        public async Task<ActionResult<int>> DeleteRelationship(int relationshipId)
        {
            var result = await _service.DeleteRelationshipAsync(relationshipId);
            return Ok(result);
        }
    }
}