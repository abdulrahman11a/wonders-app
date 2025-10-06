using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WondersAPI.Data;
using WondersAPI.Models;

namespace Ghaymah.WondersAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WondersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<WondersController> _logger;

        public WondersController(AppDbContext context, ILogger<WondersController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // -------------------- GET ALL --------------------
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<Wonder>), 200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetAllWonders()
        {
            try
            {
                var wonders = await _context.Wonders.ToListAsync();
                _logger.LogInformation("Fetched {Count} wonders from database", wonders.Count);
                return Ok(wonders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fetching all wonders");
                return StatusCode(500, new { message = "An error occurred while retrieving wonders." });
            }
        }

        // -------------------- GET BY ID --------------------
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Wonder), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetWonderById(string id)
        {
            if (!int.TryParse(id, out var wonderId))
            {
                _logger.LogWarning("Invalid id provided: {Id}", id);
                return BadRequest(new { message = "Invalid ID. Please provide a numeric ID." });
            }

            try
            {
                var wonder = await _context.Wonders.FindAsync(wonderId);
                if (wonder == null)
                {
                    _logger.LogWarning("Wonder with ID {Id} not found", wonderId);
                    return NotFound(new { message = $"Wonder with ID {wonderId} not found" });
                }

                return Ok(wonder);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fetching wonder by ID {Id}", wonderId);
                return StatusCode(500, new { message = "An error occurred while retrieving the wonder." });
            }
        }

        // -------------------- CREATE --------------------
        [HttpPost]
        [ProducesResponseType(typeof(Wonder), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> CreateWonder([FromBody] Wonder newWonder)
        {
            if (newWonder == null)
                return BadRequest(new { message = "Request body is required." });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                await _context.Wonders.AddAsync(newWonder);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created new wonder: {Name}", newWonder.Name);

                return CreatedAtAction(nameof(GetWonderById), new { id = newWonder.Id }, newWonder);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while creating wonder {Name}", newWonder.Name);
                return StatusCode(500, new { message = "An error occurred while creating the wonder." });
            }
        }

        // -------------------- UPDATE --------------------
        [HttpPut("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> UpdateWonder(string id, [FromBody] Wonder updatedWonder)
        {
            if (!int.TryParse(id, out var wonderId))
            {
                _logger.LogWarning("Invalid id provided for update: {Id}", id);
                return BadRequest(new { message = "Invalid ID. Please provide a numeric ID." });
            }

            if (updatedWonder == null)
                return BadRequest(new { message = "Request body is required." });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // If the client provided an Id in the body, ensure it matches route id
            if (updatedWonder.Id != 0 && updatedWonder.Id != wonderId)
                return BadRequest(new { message = "ID mismatch between route and body." });

            try
            {
                var existingWonder = await _context.Wonders.FindAsync(wonderId);
                if (existingWonder == null)
                {
                    _logger.LogWarning("Cannot update: wonder with ID {Id} not found", wonderId);
                    return NotFound(new { message = $"Wonder with ID {wonderId} not found" });
                }

                existingWonder.Name = updatedWonder.Name;
                existingWonder.Country = updatedWonder.Country;
                existingWonder.Era = updatedWonder.Era;
                existingWonder.Type = updatedWonder.Type;
                existingWonder.Description = updatedWonder.Description;
                existingWonder.DiscoveryYear = updatedWonder.DiscoveryYear;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Updated wonder with ID {Id}", wonderId);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while updating wonder with ID {Id}", wonderId);
                return StatusCode(500, new { message = "An error occurred while updating the wonder." });
            }
        }

        // -------------------- DELETE --------------------
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> DeleteWonder(string id)
        {
            if (!int.TryParse(id, out var wonderId))
            {
                _logger.LogWarning("Invalid id provided for delete: {Id}", id);
                return BadRequest(new { message = "Invalid ID. Please provide a numeric ID." });
            }

            try
            {
                var wonder = await _context.Wonders.FindAsync(wonderId);
                if (wonder == null)
                {
                    _logger.LogWarning("Cannot delete: wonder with ID {Id} not found", wonderId);
                    return NotFound(new { message = $"Wonder with ID {wonderId} not found" });
                }

                _context.Wonders.Remove(wonder);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted wonder with ID {Id}", wonderId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while deleting wonder with ID {Id}", wonderId);
                return StatusCode(500, new { message = "An error occurred while deleting the wonder." });
            }
        }

        // -------------------- GET RANDOM --------------------
        [HttpGet("random")]
        [ProducesResponseType(typeof(Wonder), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetRandomWonder()
        {
            try
            {
                var wonders = await _context.Wonders.ToListAsync();
                if (!wonders.Any())
                {
                    _logger.LogWarning("No wonders available for random selection");
                    return NotFound(new { message = "No wonders available." });
                }

                var randomWonder = wonders[new Random().Next(wonders.Count)];
                _logger.LogInformation("Returned random wonder: {Name}", randomWonder.Name);

                return Ok(randomWonder);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while selecting random wonder");
                return StatusCode(500, new { message = "An error occurred while retrieving a random wonder." });
            }
        }
    }
}
