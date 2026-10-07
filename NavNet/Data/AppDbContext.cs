using Microsoft.EntityFrameworkCore;
using NavNet.Models;

namespace NavNet.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Game> Games => Set<Game>();
    public DbSet<Genre> Genres => Set<Genre>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Genre>().HasData(
            new Genre { Id = 1, Name = "Action" },
            new Genre { Id = 2, Name = "Sports" },
            new Genre { Id = 3, Name = "Adventure" },
            new Genre { Id = 4, Name = "FPS" },
            new Genre { Id = 5, Name = "Simulation" },
            new Genre { Id = 6, Name = "Roguelike" },
            new Genre { Id = 7, Name = "RPG" },
            new Genre { Id = 8, Name = "Puzzle" }
        );
    }
}
