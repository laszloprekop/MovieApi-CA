using System.ComponentModel.DataAnnotations;

namespace MovieCore.Models
{
    public class Review
    {
        public int Id { get; set; }
        public required string ReviewerName { get; set; }
        public required string Comment { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; }

        // FK to Movie, and navigation property back to Movie
        public int MovieId { get; set; }
        public Movie Movie { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
