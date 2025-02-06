using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using CarparkInfoApi.Models;
using CarparkInfoApi.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace CarparkInfoApi.Controllers
{
    [ApiController]
    [Route("api/favourites")]
    public class FavouritesController : ControllerBase
    {
        private readonly ApplicationDbContext _context; // Replace YourDbContext with your actual DbContext name

        public FavouritesController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        // [Authorize]
        public async Task<ActionResult<IEnumerable<UserFavoriteCarPark>>> GetUserFavorites(int userId)
        {
            var favorites = await _context.UserFavoriteCarParks
                .Where(f => f.UserId == userId)
                .ToListAsync();



            return Ok(favorites);
        }

        [HttpPost]
        //  [Authorize]
        public async Task<ActionResult<UserFavoriteCarPark>> CreateFavorite(UserFavoriteCarPark favorite)
        {
            var exists = await _context.UserFavoriteCarParks
                .AnyAsync(f => f.UserId == favorite.UserId && f.CarParkId == favorite.CarParkId);

            if (exists)
            {
                return Conflict("This carpark is already in favorites");
            }

            _context.UserFavoriteCarParks.Add(favorite);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUserFavorites), new { userId = favorite.UserId }, favorite);
        }

    }
}