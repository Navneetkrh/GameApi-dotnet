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
            Results.Ok(await db.Games.Select(g => new GameDto(g.Id, g.Name, g.GenreId, g.Price, g.ReleaseDate))
                .ToListAsync()));

        group.MapGet("/{id}", async (int id, AppDbContext db) =>
        {
            var dto = await db.Games.Where(g => g.Id == id)
                .Select(g => new GameDto(g.Id, g.Name, g.GenreId, g.Price, g.ReleaseDate))
                .FirstOrDefaultAsync();

            return dto is null ? Results.NotFound() : Results.Ok(dto);
        }).WithName("GetGameManual");

        group.MapPost("/", async (CreateGameDto newGame, AppDbContext db) =>
        {
            if (!await db.Genres.AnyAsync(g => g.Id == newGame.GenreId))
            {
                return Results.BadRequest($"Unknown genre id {newGame.GenreId}.");
            }

            var game = new Game
            {
                Name = newGame.Name,
                GenreId = newGame.GenreId,
                Price = newGame.Price,
                ReleaseDate = newGame.ReleaseDate
            };

            db.Games.Add(game);
            await db.SaveChangesAsync();

            return Results.CreatedAtRoute(
                "GetGameManual",
                new { id = game.Id },
                new GameDto(game.Id, game.Name, game.GenreId, game.Price, game.ReleaseDate));
        });

        group.MapPut("/{id}", async (int id, FullUpdateGameDto updatedGame, AppDbContext db) =>
        {
            var game = await db.Games.FindAsync(id);
            if (game is null) return Results.NotFound();

            if (!await db.Genres.AnyAsync(g => g.Id == updatedGame.GenreId))
            {
                return Results.BadRequest($"Unknown genre id {updatedGame.GenreId}.");
            }

            game.Name = updatedGame.Name;
            game.GenreId = updatedGame.GenreId;
            game.Price = updatedGame.Price;
            game.ReleaseDate = updatedGame.ReleaseDate;

            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        group.MapPatch("/{id}", async (int id, UpdateGameDto patch, AppDbContext db) =>
        {
            var game = await db.Games.FindAsync(id);
            if (game is null) return Results.NotFound();

            if (patch.GenreId is not null
                && !await db.Genres.AnyAsync(g => g.Id == patch.GenreId))
            {
                return Results.BadRequest($"Unknown genre id {patch.GenreId}.");
            }

            if (patch.Name is not null) game.Name = patch.Name;
            if (patch.GenreId is not null) game.GenreId = patch.GenreId.Value;
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

    public static void MapGenreEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/genres");

        group.MapGet("/", async (AppDbContext db) =>
            Results.Ok(await db.Genres.AsNoTracking()
                .OrderBy(g => g.Name)
                .Select(g => new GenreDto(g.Id, g.Name))
                .ToListAsync()));
    }
}
