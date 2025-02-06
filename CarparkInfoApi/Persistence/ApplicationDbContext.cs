using Microsoft.EntityFrameworkCore;
using CarParkInfo.Models;

namespace CarParkInfo.models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<CarPark> CarParks { get; set; }
        public DbSet<CarParkType> CarParkTypes { get; set; }
        public DbSet<ParkingSystem> ParkingSystems { get; set; }
        public DbSet<ShortTermParking> ShortTermParkings { get; set; }
        public DbSet<UserFavoriteCarPark> UserFavoriteCarParks { get; set; }
    }
}

