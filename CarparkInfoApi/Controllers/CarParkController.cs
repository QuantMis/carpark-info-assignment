
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CarparkInfoApi.Models;
using CarparkInfoApi.Persistence;

namespace CarparkInfoApi.Controllers
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



    }
}

