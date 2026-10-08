using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using NavNet.Models;

namespace NavNet.Services;

public class TokenService(IConfiguration config)
{
    public string CreateToken(User user)
    {
        var key = config["Jwt:Key"]
            ?? throw new InvalidOperationException("Missing Jwt:Key. Set it via User Secrets or env.");
        if (key.Length < 32)
            throw new InvalidOperationException("Jwt:Key must be at least 32 characters.");

        var creds = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
        };

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
