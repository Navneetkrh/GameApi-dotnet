namespace NavNet.Dtos;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

public record class FullUpdateGameDto
(   [Required, StringLength(256, MinimumLength = 1)] string Name,
    [Required, StringLength(256, MinimumLength = 1)] string Genre,
    [Required, DataType(DataType.Currency), Range(0, 999999)] decimal Price,
    [Required, Range(typeof(DateOnly), "1962-10-19", "2026-12-31")] DateOnly ReleaseDate
);
