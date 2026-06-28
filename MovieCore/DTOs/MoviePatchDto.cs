using System.ComponentModel.DataAnnotations;

namespace MovieCore.DTOs;

public class MoviePatchDto
{
    [Required, StringLength(200)] public string Title { get; set; } = string.Empty;

    [Range(1888, 2100)] public int Year { get; set; }

    [Range(1, 1000)] public int Duration { get; set; }

    public string? Synopsis { get; set; }
    public string? Language { get; set; }
    public decimal Budget { get; set; }
}