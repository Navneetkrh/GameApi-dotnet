using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using NavNet.Dtos;

namespace NavNet.Controllers;

// No ControllerBase, no [ApiController].
// Returns IResult via Results.* helpers and uses [Route]/[HttpGet] for routing.
// Validation is manual via Validator.TryValidateObject.
[Route("games")]
public class GamesManualController
{
    private static List<GameDto> games = [
        new (1, "Spacewar", "Action", 9.99M, new DateOnly(1962, 10, 19)),
        new (2, "Pong", "Sports", 0.00M, new DateOnly(1972, 11, 29))
    ];

    [HttpGet]
    public IResult GetGames()
    {
        return Results.Ok(games);
    }

    [HttpGet("{id}", Name = "GetGameManual")]
    public IResult GetGame(int id)
    {
        var game = games.Find(g => g.Id == id);
        return game is null ? Results.NotFound() : Results.Ok(game);
    }

    [HttpPost]
    public IResult CreateGame([FromBody] CreateGameDto newGame)
    {
        if (!TryValidate(newGame, out var errors))
            return Results.ValidationProblem(errors);

        GameDto game = new(games.Count + 1, newGame.Name, newGame.Genre, newGame.Price, newGame.ReleaseDate);
        games.Add(game);
        return Results.CreatedAtRoute("GetGameManual", new { id = game.Id }, game);
    }

    [HttpPut("{id}")]
    public IResult UpdateGame(int id, [FromBody] FullUpdateGameDto updatedGame)
    {
        if (!TryValidate(updatedGame, out var errors))
            return Results.ValidationProblem(errors);

        var index = games.FindIndex(g => g.Id == id);
        if (index == -1) return Results.NotFound();

        games[index] = new GameDto(id, updatedGame.Name, updatedGame.Genre, updatedGame.Price, updatedGame.ReleaseDate);
        return Results.NoContent();
    }

    [HttpPatch("{id}")]
    public IResult PatchGame(int id, [FromBody] UpdateGameDto patch)
    {
        if (!TryValidate(patch, out var errors))
            return Results.ValidationProblem(errors);

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
    }

    [HttpDelete("{id}")]
    public IResult DeleteGame(int id)
    {
        var index = games.FindIndex(g => g.Id == id);
        if (index == -1) return Results.NotFound();

        games.RemoveAt(index);
        return Results.NoContent();
    }

    private static bool TryValidate(object dto, out Dictionary<string, string[]> errors)
    {
        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();
        bool isValid = Validator.TryValidateObject(dto, context, results, validateAllProperties: true);

        errors = results
            .GroupBy(r => r.MemberNames.FirstOrDefault() ?? string.Empty)
            .ToDictionary(g => g.Key, g => g.Select(r => r.ErrorMessage ?? "Invalid").ToArray());

        return isValid;
    }
}
