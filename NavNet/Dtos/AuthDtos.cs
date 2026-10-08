using System.ComponentModel.DataAnnotations;

namespace NavNet.Dtos;

public record class RegisterDto(
    [Required, StringLength(64, MinimumLength = 3)] string Username,
    [Required, StringLength(100, MinimumLength = 8)] string Password
);

public record class LoginDto(
    [Required] string Username,
    [Required] string Password
);

public record class AuthResponseDto(string Token);
