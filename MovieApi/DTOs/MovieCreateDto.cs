using System.ComponentModel.DataAnnotations;

namespace MovieApi.DTOs
{
    // POST
    public class MovieCreateDto
    {
        [Required, StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Range(1888, 2100)]
        public int Year { get; set; }

        [Required]
        public string Genre { get; set; } = string.Empty;

        [Range(1, 1000)]
        public int Duration { get; set; }
    }

    // PUT - separate DTO for updates, as some fields might be different (e.g., optional)
    public class MovieUpdateDto
    {
        [Required, StringLength(200)]
        public string Title { get; set; } = string.Empty;
        [Range(1888, 2100)]
        public int Year { get; set; }
        [Required]
        public string Genre { get; set; } = string.Empty;
        [Range(1, 1000)]
        public int Duration { get; set; }
    }
}
