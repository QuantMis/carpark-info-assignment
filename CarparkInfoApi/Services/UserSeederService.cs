using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using CarparkInfoApi.Models;
using CarparkInfoApi.Persistence;

namespace CarparkInfoApi.Services
{
    public class UserSeederService
    {
        private readonly ApplicationDbContext _context;
            private readonly IPasswordHasher<User> _passwordHasher;

        public UserSeederService(ApplicationDbContext context, IPasswordHasher<User> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher; 
        }

        public async Task SeedAsync()
        {
            if (!_context.Users.Any())
            {
                await SeedUsersAsync();
            }

        }

        private async Task SeedUsersAsync()
        {
            var users = new List<User>
        {
            new User { UserName = "user1", Email = "user1@email.com", Password = "password123" },
        };
            foreach (var user in users)
            {
                user.Password = _passwordHasher.HashPassword(user, user.Password);
            }

            _context.Users.AddRange(users);
            await _context.SaveChangesAsync();
        }


    }
}