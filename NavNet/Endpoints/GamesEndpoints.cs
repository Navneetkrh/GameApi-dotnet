using NavNet.Dtos;

namespace NavNet.Endpoints;

public static class GamesEndpoints
{
    private static List<GameDto> games = [
        new (1, "Spacewar", "Action", 9.99M, new DateOnly(1962, 10, 19)),
        new (2, "Pong", "Sports", 0.00M, new DateOnly(1972, 11, 29))
    ];

    public static void MapGamesEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/games");

        group.MapGet("/", () => Results.Ok(games));

        group.MapGet("/{id}", (int id) =>
        {
            var game = games.Find(g => g.Id == id);
            return game is null ? Results.NotFound() : Results.Ok(game);
        }).WithName("GetGameManual");

        group.MapPost("/", (CreateGameDto newGame) =>
        {
            GameDto game = new(games.Count + 1, newGame.Name, newGame.Genre, newGame.Price, newGame.ReleaseDate);
            games.Add(game);
            return Results.CreatedAtRoute("GetGameManual", new { id = game.Id }, game);
        });

        group.MapPut("/{id}", (int id, FullUpdateGameDto updatedGame) =>
        {
            var index = games.FindIndex(g => g.Id == id);
            if (index == -1) return Results.NotFound();

            games[index] = new GameDto(id, updatedGame.Name, updatedGame.Genre, updatedGame.Price, updatedGame.ReleaseDate);
            return Results.NoContent();
        });

        group.MapPatch("/{id}", (int id, UpdateGameDto patch) =>
        {
            var index = games.FindIndex(g => g.Id == id);
            if (index == -1) return Results.NotFound();

            var existing = games[index];
            games[index] = new GameDto(
                id,
                patch.Name ?? existing.Name,
                patch.Genre ?? existing.Genre,
                patch.Price ?? existing.Price,
                patch.ReleaseDate ?? existing.ReleaseDate
            );
            return Results.NoContent();
        });

        group.MapDelete("/{id}", (int id) =>
        {
            var index = games.FindIndex(g => g.Id == id);
            if (index == -1) return Results.NotFound();

            games.RemoveAt(index);
            return Results.NoContent();
        });
    }
}
