using Microsoft.EntityFrameworkCore;
using NavNet.Models;

namespace NavNet.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Game> Games => Set<Game>();
    public DbSet<Genre> Genres => Set<Genre>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<User>().HasIndex(u => u.Username).IsUnique();
        mb.Entity<User>().Property(u => u.Username).HasMaxLength(64);
    }
}
