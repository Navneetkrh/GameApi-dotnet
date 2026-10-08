using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace NavNet.Dtos;

public record class GameDto (
    int Id,
    string Name,
    int GenreId,
    decimal Price,
    DateOnly ReleaseDate
);
