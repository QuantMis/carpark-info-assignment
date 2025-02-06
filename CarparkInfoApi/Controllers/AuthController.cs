using CarParkInfo.models;
using Microsoft.AspNetCore.Authorization;
using CarparkInfoApi.Models;
using CarparkInfoApi.Services;

using Microsoft.AspNetCore.Mvc;

namespace CarparkInfoApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly JwtService _jwtService;

        public AuthController(ApplicationDbContext context, JwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] User loginUser)
        {
            var user = _context.Users
                .FirstOrDefault(u => u.Name == loginUser.Name && u.Password == loginUser.Password);

            if (user == null)
                return Unauthorized("Invalid username or password");

            var token = _jwtService.GenerateJwtToken(user);

            return Ok(new { Token = token });
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            return Ok("Logged out successfully.");
        }

          [HttpGet("profile")]
        [Authorize]
        public IActionResult GetUserProfile()
        {
            var username = User.Identity.Name;

            var user = _context.Users.FirstOrDefault(u => u.Name == username);

            if (user == null)
                return NotFound("User not found.");

            return Ok(user);
        }
    }
}