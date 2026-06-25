using MovieApi.Models;

namespace MovieApi.DTOs
{
    public class MovieDetailDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int Year { get; set; }
        public string Genre { get; set; } = string.Empty;
        public int Duration { get; set; }

        // flattened from the 1:1 MovieDetails
        public string? Synopsis { get; set; }
        public string? Language { get; set; }
        public decimal? Budget { get; set; }
        public List<ReviewDto> Reviews { get; set; } = new();
        public List<ActorDto> Actors { get; set; } = new();
    }
}
