namespace MovieCore.Models
{
    public class Movie
    {
        public int Id { get; set; }
        public required string Title { get; set; }
        public int Year { get; set; }
        public ICollection<Genre> Genres { get; set; } = new List<Genre>();
        public int Duration { get; set; }

        // 1:1 Movie is the principal, MovieDetails is the dependent (holds the FX)
        public MovieDetails? Details { get; set; }

        // 1:M One movie has many reviews, but a review belongs to one movie
        public ICollection<Review> Reviews { get; set; } = new List<Review>();

        // N:M many to many  - the join is the explicit entity MovieActor.
        // Two roads to the same destination, Actor for "who is in it",
        // Cast when the role on the relationship matters.
        public ICollection<Actor> Actors { get; set; } = new List<Actor>();
        public ICollection<MovieActor> Cast { get; set; } = new List<MovieActor>();
    }
}
