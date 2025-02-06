using Microsoft.AspNetCore.Authorization;
using CarparkInfoApi.Models;
using CarparkInfoApi.Services;
using Microsoft.AspNetCore.Mvc;
using CarparkInfoApi.Persistence;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks.Dataflow;

namespace CarparkInfoApi.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;

        private readonly JwtService _jwtService;

        public AuthController(ApplicationDbContext context, IPasswordHasher<User> passwordHasher, JwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
            _passwordHasher = passwordHasher;

        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] User loginUser)
        {
            var user = _context.Users.FirstOrDefault(u => u.UserName == loginUser.UserName);

            if (user == null)
                return Unauthorized("Invalid username or password");

            var result = _passwordHasher.VerifyHashedPassword(user, user.Password, loginUser.Password);

            if (result == PasswordVerificationResult.Failed)
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
        // [Authorize]
        public IActionResult GetUserProfile()
        {
            var username = User.Identity.Name;
            Console.WriteLine($"User ID claim: {User.Identity}");

            var user = _context.Users.FirstOrDefault(u => u.UserName == username);

            if (user == null)
                return NotFound("User not found.");

            return Ok(user);
        }
    }
}