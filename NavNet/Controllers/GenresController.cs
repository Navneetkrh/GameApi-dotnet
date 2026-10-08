using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NavNet.Data;
using NavNet.Dtos;

namespace NavNet.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GenresController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<GenreDto>>> GetGenres()
    {
        return await db.Genres.AsNoTracking()
            .OrderBy(g => g.Name)
            .Select(g => new GenreDto(g.Id, g.Name))
            .ToListAsync();
    }
}
