namespace NavNet.Dtos;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

public record class UpdateGameDto
(    [StringLength(256, MinimumLength = 1)] string? Name = null,
    [StringLength(256, MinimumLength = 1)] string? Genre = null,
    [DataType(DataType.Currency), Range(0, 999999)] decimal? Price = null,
    [Range(typeof(DateOnly), "1962-10-19", "2026-12-31")] DateOnly? ReleaseDate = null
);
