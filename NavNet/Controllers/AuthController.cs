using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NavNet.Data;
using NavNet.Dtos;
using NavNet.Models;
using NavNet.Services;

namespace NavNet.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(AppDbContext db, TokenService tokens) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto dto)
    {
        var username = dto.Username.Trim();
        if (await db.Users.AnyAsync(u => u.Username == username))
            return Conflict($"Username '{username}' is taken.");

        var user = new User { Username = username, PasswordHash = PasswordHelper.Hash(dto.Password) };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return Ok(new AuthResponseDto(tokens.CreateToken(user)));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == dto.Username.Trim());
        if (user is null || !PasswordHelper.Verify(dto.Password, user.PasswordHash))
            return Unauthorized();

        return Ok(new AuthResponseDto(tokens.CreateToken(user)));
    }
}
