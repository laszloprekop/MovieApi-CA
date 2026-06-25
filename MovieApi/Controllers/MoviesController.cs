using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MovieCore.DomainContracts;
using MovieCore.DTOs;
using MovieCore.Models;
using MovieData;

namespace MovieApi.Controllers;

[ApiController]
[Route("api/movies")]
public class MoviesController(IUnitOfWork iuw, MovieContext context) : ControllerBase
{
    // GET: api/movies
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MovieDto>>> GetMovies(
        [FromQuery] string? genre,
        [FromQuery] int? year,
        [FromQuery] string? actor)
    {
        var query = await iuw.Movies.GetAllAsync();

        if (!string.IsNullOrWhiteSpace(genre)) query = query.Where(m => m.Genre == genre);
        if (year is not null) query = query.Where(m => m.Year == year);
        if (!string.IsNullOrWhiteSpace(actor)) query = query.Where(m => m.Actors.Any(a => a.Name == actor));

        var movies = query
            .Select(m => new MovieDto
            {
                Id = m.Id, Title = m.Title, Year = m.Year, Genre = m.Genre, Duration = m.Duration
            }).ToList();
        return Ok(movies);
    }

    // GET: api/movies/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<MovieDto>> GetMovie(int id)
    {
        var movie = await iuw.Movies.GetAsync(id);

        if (movie is null) return NotFound();

        return Ok(new MovieDto
        {
            Id = movie.Id, Title = movie.Title, Year = movie.Year, Genre = movie.Genre, Duration = movie.Duration
        });
    }

    // GET /api/movies/{id}/details
    [HttpGet("{id:int}/details")]
    public async Task<ActionResult<MovieDetailDto>> GetMovieDetailsAsync(int id)
    {
        var movie = await iuw.Movies.GetWithDetailsAsync(id);
        if (movie is null) return NotFound();
        
        var dto = new MovieDetailDto
        {
            Id = movie.Id, Title = movie.Title, Year = movie.Year, Genre = movie.Genre, Duration = movie.Duration,
            Synopsis = movie.Details?.Synopsis,
            Language = movie.Details?.Language,
            Budget = movie.Details?.Budget ?? 0,
            Reviews = movie.Reviews.Select(r => new ReviewDto
            {
                Id = r.Id, ReviewerName = r.ReviewerName, Comment = r.Comment, Rating = r.Rating
            }).ToList(),
            Actors = movie.Actors.Select(a => new ActorDto
            {
                Id = a.Id, Name = a.Name, BirthYear = a.BirthYear
            }).ToList()
        };
        return Ok(dto);
    }

    // POST /api/movies
    [HttpPost]
    public async Task<ActionResult<MovieDto>> CreateMovie(MovieCreateDto dto)
    {
        var movie = new Movie
        {
            Title = dto.Title,
            Year = dto.Year,
            Genre = dto.Genre,
            Duration = dto.Duration
        };
        iuw.Movies.Add(movie);
        await iuw.CompleteAsync();
        var result = new MovieDto
        {
            Id = movie.Id, Title = movie.Title, Year = movie.Year, Genre = movie.Genre, Duration = movie.Duration
        };
        return CreatedAtAction(nameof(GetMovie), new { id = movie.Id }, result);
    }

    // PUT: api/movies/{id}
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateMovie(int id, MovieUpdateDto dto)
    {
        var movie = await iuw.Movies.GetAsync(id);
        if (movie is null) return NotFound();
        movie.Title = dto.Title;
        movie.Year = dto.Year;
        movie.Genre = dto.Genre;
        movie.Duration = dto.Duration;
        await iuw.CompleteAsync();
        return NoContent();
    }

    // DELETE: api/movies/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteMovie(int id)
    {
        var movie = await iuw.Movies.GetAsync(id);
        if (movie is null) return NotFound();

        iuw.Movies.Remove(movie); // cascade delete Reviews and MovieDetails
        await iuw.CompleteAsync();

        return NoContent();
    }
}