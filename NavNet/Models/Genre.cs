using System.ComponentModel.DataAnnotations;

namespace NavNet.Models;

public class Genre
{
    public int Id { get; set; }

    [MaxLength(256)]
    public string Name { get; set; } = string.Empty;

    public List<Game> Games { get; set; } = [];
}
