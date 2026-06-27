namespace MovieCore.Models;

public class Genre
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public ICollection<Movie> Movies { get; set; } = new List<Movie>();
}