using Microsoft.AspNetCore.Mvc;
using MovieContracts;
using MovieCore.DTOs;

namespace MoviePresentation.Controllers;

[ApiController]
[Route("api/movies")]
public class MoviesController(IServiceManager services) : ControllerBase
{
    // GET: api/movies
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MovieDto>>> GetMovies(
        [FromQuery] string? genre,
        [FromQuery] int? year,
        [FromQuery] string? actor) =>
        Ok(await services.MovieService.GetAllAsync(genre, year, actor));

    // GET: api/movies/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<MovieDto>> GetMovie(int id) =>
        Ok(await services.MovieService.GetAsync(id));

    // GET /api/movies/{id}/details
    [HttpGet("{id:int}/details")]
    public async Task<ActionResult<MovieDetailDto>> GetMovieDetails(int id) =>
        Ok(await services.MovieService.GetDetailsAsync(id));

    // POST /api/movies
    [HttpPost]
    public async Task<ActionResult<MovieDto>> CreateMovie(MovieCreateDto dto)
    {
        var created = await services.MovieService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetMovie), new { id = created.Id }, created);
    }

    // PUT: api/movies/{id}
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateMovie(int id, MovieUpdateDto dto)
    {
        await services.MovieService.UpdateAsync(id, dto);
        return NoContent();
    }

    // DELETE: api/movies/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteMovie(int id)
    {
        await services.MovieService.DeleteAsync(id);
        return NoContent();
    }
}