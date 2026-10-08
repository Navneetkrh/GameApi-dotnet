using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NavNet.Data;
using NavNet.Dtos;
using NavNet.Models;

namespace NavNet.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GamesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<GameDto>>> GetGames()
    {
        return await db.Games.Select(g => new GameDto(g.Id, g.Name, g.GenreId, g.Price, g.ReleaseDate))
            .ToListAsync();
    }

    [HttpGet("{id}", Name = "GetGame")]
    public async Task<ActionResult<GameDto>> GetGame(int id)
    {
        var dto = await db.Games.Where(g => g.Id == id)
            .Select(g => new GameDto(g.Id, g.Name, g.GenreId, g.Price, g.ReleaseDate))
            .FirstOrDefaultAsync();

        if (dto is null)
        {
            return NotFound();
        }

        return dto;
    }

    [HttpPost]
    public async Task<ActionResult<GameDto>> CreateGame(CreateGameDto newGame)
    {
        if (!await db.Genres.AnyAsync(g => g.Id == newGame.GenreId))
        {
            return BadRequest($"Unknown genre id {newGame.GenreId}.");
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

        var dto = new GameDto(game.Id, game.Name, game.GenreId, game.Price, game.ReleaseDate);
        return CreatedAtRoute("GetGame", new { id = game.Id }, dto);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateGame(int id, FullUpdateGameDto updatedGame)
    {
        var game = await db.Games.FindAsync(id);

        if (game is null)
        {
            return NotFound();
        }

        if (!await db.Genres.AnyAsync(g => g.Id == updatedGame.GenreId))
        {
            return BadRequest($"Unknown genre id {updatedGame.GenreId}.");
        }

        game.Name = updatedGame.Name;
        game.GenreId = updatedGame.GenreId;
        game.Price = updatedGame.Price;
        game.ReleaseDate = updatedGame.ReleaseDate;

        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> PatchGame(int id, UpdateGameDto patch)
    {
        var game = await db.Games.FindAsync(id);

        if (game is null)
        {
            return NotFound();
        }

        if (patch.GenreId is not null
            && !await db.Genres.AnyAsync(g => g.Id == patch.GenreId))
        {
            return BadRequest($"Unknown genre id {patch.GenreId}.");
        }

        if (patch.Name is not null) game.Name = patch.Name;
        if (patch.GenreId is not null) game.GenreId = patch.GenreId.Value;
        if (patch.Price is not null) game.Price = patch.Price.Value;
        if (patch.ReleaseDate is not null) game.ReleaseDate = patch.ReleaseDate.Value;

        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteGame(int id)
    {
        var game = await db.Games.FindAsync(id);

        if (game is null)
        {
            return NotFound();
        }

        db.Games.Remove(game);
        await db.SaveChangesAsync();

        return NoContent();
    }
}
