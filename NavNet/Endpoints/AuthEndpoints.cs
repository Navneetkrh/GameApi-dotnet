using Microsoft.EntityFrameworkCore;
using NavNet.Data;
using NavNet.Dtos;
using NavNet.Models;
using NavNet.Services;

namespace NavNet.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/auth");

        group.MapPost("/register", async (RegisterDto dto, AppDbContext db, TokenService tokens) =>
        {
            var username = dto.Username.Trim();
            if (await db.Users.AnyAsync(u => u.Username == username))
                return Results.Conflict($"Username '{username}' is taken.");

            var user = new User { Username = username, PasswordHash = PasswordHelper.Hash(dto.Password) };
            db.Users.Add(user);
            await db.SaveChangesAsync();
            return Results.Ok(new AuthResponseDto(tokens.CreateToken(user)));
        });

        group.MapPost("/login", async (LoginDto dto, AppDbContext db, TokenService tokens) =>
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Username == dto.Username.Trim());
            if (user is null || !PasswordHelper.Verify(dto.Password, user.PasswordHash))
                return Results.Unauthorized();

            return Results.Ok(new AuthResponseDto(tokens.CreateToken(user)));
        });
    }
}
