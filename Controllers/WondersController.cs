using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WondersAPI.Data;
using WondersAPI.Models;

namespace Ghaymah.WondersAPI.Controllers
{
    /// <summary>
    /// API controller to manage Wonders (CRUD + Random).
    /// </summary>
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
        /// <summary>
        /// Get all wonders.
        /// </summary>
        /// <returns>List of all wonders.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<Wonder>), 200)]
        public async Task<IActionResult> GetAllWonders()
        {
            var wonders = await FetchAllWonders();
            LogFetchedWonders(wonders.Count);
            return Ok(wonders);
        }

        private async Task<List<Wonder>> FetchAllWonders() =>
            await _context.Wonders.ToListAsync();

        private void LogFetchedWonders(int count) =>
            _logger.LogInformation("Fetched {Count} wonders from database", count);

        // -------------------- GET BY ID --------------------
        /// <summary>
        /// Get a specific wonder by its ID.
        /// </summary>
        /// <param name="wonderId">The ID of the wonder.</param>
        /// <returns>The wonder with the specified ID.</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Wonder), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetWonderById(int wonderId)
        {
            var wonder = await FindWonderOrNotFound(wonderId);
            if (wonder is null) return NotFound(new { message = $"Wonder with ID {wonderId} not found" });

            return Ok(wonder);
        }

        private async Task<Wonder?> FindWonderOrNotFound(int wonderId) =>
            await _context.Wonders.FindAsync(wonderId);

        // -------------------- CREATE --------------------
        /// <summary>
        /// Create a new wonder.
        /// </summary>
        /// <param name="newWonder">The wonder to create.</param>
        /// <returns>The created wonder.</returns>
        [HttpPost]
        [ProducesResponseType(typeof(Wonder), 201)]
        public async Task<IActionResult> CreateWonder([FromBody] Wonder newWonder) =>
            await SaveNewWonder(newWonder);

        private async Task<IActionResult> SaveNewWonder(Wonder wonder)
        {
            _context.Wonders.Add(wonder);
            await _context.SaveChangesAsync();
            LogCreatedWonder(wonder.Name);
            return CreatedAtAction(nameof(GetWonderById), new { wonderId = wonder.Id }, wonder);
        }

        private void LogCreatedWonder(string name) =>
            _logger.LogInformation("Created new wonder: {Name}", name);

        // -------------------- UPDATE --------------------
        /// <summary>
        /// Update an existing wonder.
        /// </summary>
        /// <param name="wonderId">The ID of the wonder to update.</param>
        /// <param name="updatedWonder">The updated wonder data.</param>
        [HttpPut("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateWonder(int wonderId, [FromBody] Wonder updatedWonder)
        {
            var existingWonder = await FindWonderOrNotFound(wonderId);
            if (existingWonder is null) return NotFound();

            return await SaveUpdatedWonder(existingWonder, updatedWonder);
        }

        private async Task<IActionResult> SaveUpdatedWonder(Wonder target, Wonder source)
        {
            CopyWonderValues(target, source);
            await _context.SaveChangesAsync();
            LogUpdatedWonder(target.Id);
            return NoContent();
        }

        private void CopyWonderValues(Wonder target, Wonder source)
        {
            target.Name = source.Name;
            target.Country = source.Country;
            target.Era = source.Era;
            target.Type = source.Type;
            target.Description = source.Description;
            target.DiscoveryYear = source.DiscoveryYear;
        }

        private void LogUpdatedWonder(int wonderId) =>
            _logger.LogInformation("Updated wonder with ID {Id}", wonderId);

        // -------------------- DELETE --------------------
        /// <summary>
        /// Delete a wonder by ID.
        /// </summary>
        /// <param name="wonderId">The ID of the wonder to delete.</param>
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteWonder(int wonderId)
        {
            var wonder = await FindWonderOrNotFound(wonderId);
            if (wonder is null) return NotFound();

            await RemoveWonder(wonder);
            return NoContent();
        }

        private async Task RemoveWonder(Wonder wonder)
        {
            _context.Wonders.Remove(wonder);
            await _context.SaveChangesAsync();
            LogDeletedWonder(wonder.Id);
        }

        private void LogDeletedWonder(int wonderId) =>
            _logger.LogInformation("Deleted wonder with ID {Id}", wonderId);

        // -------------------- GET RANDOM --------------------
        /// <summary>
        /// Get a random wonder.
        /// </summary>
        /// <returns>A randomly selected wonder.</returns>
        [HttpGet("random")]
        [ProducesResponseType(typeof(Wonder), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetRandomWonder()
        {
            var allWonders = await FetchAllWonders();
            if (!allWonders.Any()) return NotFound();

            var randomWonder = SelectRandomWonder(allWonders);
            LogRandomWonder(randomWonder.Name);
            return Ok(randomWonder);
        }

        private Wonder SelectRandomWonder(List<Wonder> wonders) =>
            wonders[new Random().Next(wonders.Count)];

        private void LogRandomWonder(string name) =>
            _logger.LogInformation("Returned random wonder: {Name}", name);
    }
}
