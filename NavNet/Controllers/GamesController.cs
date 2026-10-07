using Microsoft.AspNetCore.Mvc;
using NavNet.Dtos;

namespace NavNet.Controllers;

[ApiController]
[Route("[controller]")]
public class GamesController : ControllerBase
{
    private static List<GameDto> games = [
        new (1, "Spacewar",          "Action",     9.99M,  new DateOnly(1962, 10, 19)),
        new (2, "Pong",              "Sports",     0.00M,  new DateOnly(1972, 11, 29)),
        new (3, "The Legend of Zelda","Adventure", 59.99M, new DateOnly(1986, 2, 21)),
        new (4, "Doom",              "FPS",        19.99M, new DateOnly(1993, 12, 10)),
        new (5, "Half-Life 2",       "FPS",        9.99M,  new DateOnly(2004, 11, 16)),
        new (6, "Stardew Valley",    "Simulation", 14.99M, new DateOnly(2016, 2, 26)),
        new (7, "Hades",             "Roguelike",  24.99M, new DateOnly(2020, 9, 17)),
        new (8, "Elden Ring",        "RPG",        59.99M, new DateOnly(2022, 2, 25))
    ];

    [HttpGet]
    public ActionResult<List<GameDto>> GetGames()
    {
        return games;
    }

    [HttpGet("{id}", Name = "GetGame")]
    public ActionResult<GameDto> GetGame(int id)
    {
        var game = games.Find(g => g.Id == id);

        if (game is null)
        {
            return NotFound();
        }

        return game;
    }

    [HttpPost]
    public ActionResult<GameDto> CreateGame(CreateGameDto newGame)
    {
        GameDto game = new(
            games.Count + 1,
            newGame.Name,
            newGame.Genre,
            newGame.Price,
            newGame.ReleaseDate
        );

        games.Add(game);

        return CreatedAtRoute("GetGame", new { id = game.Id }, game);
    }

    [HttpPut("{id}")]
    public IActionResult UpdateGame(int id, FullUpdateGameDto updatedGame)
    {
        var index = games.FindIndex(g => g.Id == id);

        if (index == -1)
        {
            return NotFound();
        }

        games[index] = new GameDto(
            id,
            updatedGame.Name,
            updatedGame.Genre,
            updatedGame.Price,
            updatedGame.ReleaseDate
        );

        return NoContent();
    }

    [HttpPatch("{id}")]
    public IActionResult PatchGame(int id, UpdateGameDto patch)
    {
        var index = games.FindIndex(g => g.Id == id);

        if (index == -1)
        {
            return NotFound();
        }

        var existing = games[index];
        games[index] = new GameDto(
            id,
            patch.Name ?? existing.Name,
            patch.Genre ?? existing.Genre,
            patch.Price ?? existing.Price,
            patch.ReleaseDate ?? existing.ReleaseDate
        );

        return NoContent();
    }

    [HttpDelete("{id}")]
    public IActionResult DeleteGame(int id)
    {
        var index = games.FindIndex(g => g.Id == id);

        if (index == -1)
        {
            return NotFound();
        }

        games.RemoveAt(index);

        return NoContent();
    }
}