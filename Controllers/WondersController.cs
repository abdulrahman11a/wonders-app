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

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var wonders = await _context.Wonders.ToListAsync();
            _logger.LogInformation("Fetched {Count} wonders from database", wonders.Count);
            return Ok(wonders);
        }


        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var wonder = await _context.Wonders.FindAsync(id);
            if (wonder == null)
            {
                _logger.LogWarning("Wonder with ID {Id} not found", id);
                return NotFound(new { message = $"Wonder with ID {id} not found" });
            }

            return Ok(wonder);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Wonder wonder)
        {
            _context.Wonders.Add(wonder);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Created new wonder: {Name}", wonder.Name);
            return CreatedAtAction(nameof(GetById), new { id = wonder.Id }, wonder);
        }

 
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, Wonder updatedWonder)
        {
            var existing = await _context.Wonders.FindAsync(id);
            if (existing == null)
            {
                _logger.LogWarning("Update failed: Wonder with ID {Id} not found", id);
                return NotFound();
            }

            existing.Name = updatedWonder.Name;
            existing.Country = updatedWonder.Country;
            existing.Era = updatedWonder.Era;
            existing.Type = updatedWonder.Type;
            existing.Description = updatedWonder.Description;
            existing.DiscoveryYear = updatedWonder.DiscoveryYear;

            await _context.SaveChangesAsync();
            _logger.LogInformation("Updated wonder with ID {Id}", id);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var wonder = await _context.Wonders.FindAsync(id);
            if (wonder == null)
            {
                _logger.LogWarning("Delete failed: Wonder with ID {Id} not found", id);
                return NotFound();
            }

            _context.Wonders.Remove(wonder);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Deleted wonder with ID {Id}", id);
            return NoContent();
        }


        [HttpGet("random")]
        public async Task<IActionResult> GetRandom()
        {
            var wonders = await _context.Wonders.ToListAsync();
            if (!wonders.Any())
                return NotFound();

            var random = wonders[new Random().Next(wonders.Count)];
            _logger.LogInformation("Returned random wonder: {Name}", random.Name);
            return Ok(random);
        }
    }
}
