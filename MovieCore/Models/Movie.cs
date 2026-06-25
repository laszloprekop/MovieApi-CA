namespace MovieCore.Models
{
    public class Movie
    {
        public int Id { get; set; }
        public required string Title { get; set; }
        public int Year { get; set; }
        public required string Genre { get; set; }
        public int Duration { get; set; }

        // 1:1 Movie is the principal, MovieDetails is the dependent (holds the FX)
        public MovieDetails? Details { get; set; }

        // 1:M One movie has many reviews, but a review belongs to one movie
        public ICollection<Review> Reviews { get; set; } = new List<Review>();

        //N:M many to many via implicit join table MovieActor
        public ICollection<Actor> Actors { get; set; } = new List<Actor>();

    }
}
