namespace NavNet.Models;

public class Game
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int GenreId { get; set; }
    public Genre Genre { get; set; } = null!;
    public decimal Price { get; set; }
    public DateOnly ReleaseDate { get; set; }
}
