using Microsoft.EntityFrameworkCore;
using NavNet.Models;

namespace NavNet.Data;

public static class DataExtensions
{
    public static void MigrateDb(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        dbContext.Database.Migrate();

        if (!dbContext.Genres.Any())
        {
            dbContext.Genres.AddRange(
                new Genre { Name = "Action" },
                new Genre { Name = "Sports" },
                new Genre { Name = "Adventure" },
                new Genre { Name = "FPS" },
                new Genre { Name = "Simulation" },
                new Genre { Name = "Roguelike" },
                new Genre { Name = "RPG" },
                new Genre { Name = "Puzzle" }
            );
            dbContext.SaveChanges();
        }

        if (!dbContext.Games.Any())
        {
            var genreIds = dbContext.Genres.ToDictionary(g => g.Name, g => g.Id);

            dbContext.Games.AddRange(
                new Game { Name = "Spacewar", GenreId = genreIds["Action"], Price = 9.99M, ReleaseDate = new DateOnly(1962, 10, 19) },
                new Game { Name = "Pong", GenreId = genreIds["Sports"], Price = 0.00M, ReleaseDate = new DateOnly(1972, 11, 29) },
                new Game { Name = "The Legend of Zelda", GenreId = genreIds["Adventure"], Price = 59.99M, ReleaseDate = new DateOnly(1986, 2, 21) },
                new Game { Name = "Doom", GenreId = genreIds["FPS"], Price = 19.99M, ReleaseDate = new DateOnly(1993, 12, 10) },
                new Game { Name = "Half-Life 2", GenreId = genreIds["FPS"], Price = 9.99M, ReleaseDate = new DateOnly(2004, 11, 16) },
                new Game { Name = "Stardew Valley", GenreId = genreIds["Simulation"], Price = 14.99M, ReleaseDate = new DateOnly(2016, 2, 26) },
                new Game { Name = "Hades", GenreId = genreIds["Roguelike"], Price = 24.99M, ReleaseDate = new DateOnly(2020, 9, 17) },
                new Game { Name = "Elden Ring", GenreId = genreIds["RPG"], Price = 59.99M, ReleaseDate = new DateOnly(2022, 2, 25) }
            );
            dbContext.SaveChanges();
        }
    }
}
