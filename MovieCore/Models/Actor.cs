namespace MovieCore.Models
{
    public class Actor
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public int BirthYear { get; set; }
        // Mirror collection for the N:M relationship with Movie
        public List<Movie> Movies { get; set; } = new();
    }
}
