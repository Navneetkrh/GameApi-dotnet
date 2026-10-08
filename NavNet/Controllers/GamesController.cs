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
        return await db.Games.Include(g => g.Genre)
            .Select(g => new GameDto(g.Id, g.Name, g.Genre.Name, g.Price, g.ReleaseDate))
            .ToListAsync();
    }

    [HttpGet("{id}", Name = "GetGame")]
    public async Task<ActionResult<GameDto>> GetGame(int id)
    {
        var dto = await db.Games.Include(g => g.Genre)
            .Where(g => g.Id == id)
            .Select(g => new GameDto(g.Id, g.Name, g.Genre.Name, g.Price, g.ReleaseDate))
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
        var genre = await GetOrCreateGenreAsync(newGame.Genre);

        var game = new Game
        {
            Name = newGame.Name,
            Genre = genre,
            Price = newGame.Price,
            ReleaseDate = newGame.ReleaseDate
        };

        db.Games.Add(game);
        await db.SaveChangesAsync();

        var dto = new GameDto(game.Id, game.Name, genre.Name, game.Price, game.ReleaseDate);
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

        game.Name = updatedGame.Name;
        game.Genre = await GetOrCreateGenreAsync(updatedGame.Genre);
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

        if (patch.Name is not null) game.Name = patch.Name;
        if (patch.Genre is not null) game.Genre = await GetOrCreateGenreAsync(patch.Genre);
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

    private async Task<Genre> GetOrCreateGenreAsync(string name)
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
