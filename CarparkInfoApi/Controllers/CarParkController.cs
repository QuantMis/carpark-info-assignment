using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YourNamespace.Data;
using YourNamespace.Models;

namespace CarParkInfo.Controllers
{
    [Route("api/carparks")]
    [ApiController]
    public class CarParkController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CarParkController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get a list of car parks with filters
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CarPark>>> GetCarParks(
            [FromQuery] bool? free_parking,
            [FromQuery] bool? night_parking,
            [FromQuery] float? max_height)
        {
            var query = _context.CarParks.AsQueryable();

            if (free_parking.HasValue)
                query = query.Where(c => c.FreeParking == free_parking.Value);

            if (night_parking.HasValue)
                query = query.Where(c => c.NightParking == night_parking.Value);

            if (max_height.HasValue)
                query = query.Where(c => c.GantryHeight >= max_height.Value);

            return await query.ToListAsync();
        }

        /// <summary>
        /// Add a car park to a user's favorites.
        /// </summary>
        [HttpPost("favorites")]
        public async Task<IActionResult> AddFavoriteCarPark([FromBody] UserFavoriteCarPark favorite)
        {
            if (!_context.CarParks.Any(c => c.Id == favorite.CarParkId))
            {
                return NotFound(new { message = "Car park not found" });
            }

            if (_context.UserFavoriteCarParks.Any(f => f.UserId == favorite.UserId && f.CarParkId == favorite.CarParkId))
            {
                return BadRequest(new { message = "Car park already in favorites" });
            }

            _context.UserFavoriteCarParks.Add(favorite);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUserFavorites), new { userId = favorite.UserId }, favorite);
        }

        /// <summary>
        /// Get a user's favorite car parks.
        /// </summary>
        [HttpGet("favorites/{userId}")]
        public async Task<ActionResult<IEnumerable<CarPark>>> GetUserFavorites(int userId)
        {
            var favorites = await _context.UserFavoriteCarParks
                .Where(f => f.UserId == userId)
                .Select(f => f.CarParkId)
                .ToListAsync();

            var carParks = await _context.CarParks
                .Where(c => favorites.Contains(c.Id))
                .ToListAsync();

            return carParks;
        }
    }
}

