using Microsoft.EntityFrameworkCore;
using NavNet.Data;
using NavNet.Dtos;
using NavNet.Models;

namespace NavNet.Endpoints;

public static class GamesEndpoints
{
    public static void MapGamesEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/games");

        group.MapGet("/", async (AppDbContext db) =>
            Results.Ok(await db.Games.Include(g => g.Genre)
                .Select(g => new GameDto(g.Id, g.Name, g.Genre.Name, g.Price, g.ReleaseDate))
                .ToListAsync()));

        group.MapGet("/{id}", async (int id, AppDbContext db) =>
        {
            var dto = await db.Games.Include(g => g.Genre)
                .Where(g => g.Id == id)
                .Select(g => new GameDto(g.Id, g.Name, g.Genre.Name, g.Price, g.ReleaseDate))
                .FirstOrDefaultAsync();

            return dto is null ? Results.NotFound() : Results.Ok(dto);
        }).WithName("GetGameManual");

        group.MapPost("/", async (CreateGameDto newGame, AppDbContext db) =>
        {
            var genre = await GetOrCreateGenreAsync(db, newGame.Genre);

            var game = new Game
            {
                Name = newGame.Name,
                Genre = genre,
                Price = newGame.Price,
                ReleaseDate = newGame.ReleaseDate
            };

            db.Games.Add(game);
            await db.SaveChangesAsync();

            return Results.CreatedAtRoute(
                "GetGameManual",
                new { id = game.Id },
                new GameDto(game.Id, game.Name, genre.Name, game.Price, game.ReleaseDate));
        });

        group.MapPut("/{id}", async (int id, FullUpdateGameDto updatedGame, AppDbContext db) =>
        {
            var game = await db.Games.FindAsync(id);
            if (game is null) return Results.NotFound();

            game.Name = updatedGame.Name;
            game.Genre = await GetOrCreateGenreAsync(db, updatedGame.Genre);
            game.Price = updatedGame.Price;
            game.ReleaseDate = updatedGame.ReleaseDate;

            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        group.MapPatch("/{id}", async (int id, UpdateGameDto patch, AppDbContext db) =>
        {
            var game = await db.Games.FindAsync(id);
            if (game is null) return Results.NotFound();

            if (patch.Name is not null) game.Name = patch.Name;
            if (patch.Genre is not null) game.Genre = await GetOrCreateGenreAsync(db, patch.Genre);
            if (patch.Price is not null) game.Price = patch.Price.Value;
            if (patch.ReleaseDate is not null) game.ReleaseDate = patch.ReleaseDate.Value;

            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        group.MapDelete("/{id}", async (int id, AppDbContext db) =>
        {
            var game = await db.Games.FindAsync(id);
            if (game is null) return Results.NotFound();

            db.Games.Remove(game);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }

    private static async Task<Genre> GetOrCreateGenreAsync(AppDbContext db, string name)
    {
        var genre = await db.Genres.FirstOrDefaultAsync(g => g.Name == name);

        if (genre is null)
        {
            genre = new Genre { Name = name };
            db.Genres.Add(genre);
        }

        return genre;
    }
}
