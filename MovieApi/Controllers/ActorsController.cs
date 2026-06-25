using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MovieCore.DTOs;
using MovieCore.Models;
using MovieData;

namespace MovieApi.Controllers;

[ApiController]
[Route("api")]
public class ActorsController : ControllerBase
{
    private readonly MovieContext _context;
    public ActorsController(MovieContext context) => _context = context;

    // GET: api/actors
    [HttpGet("actors")]
    public async Task<ActionResult<IEnumerable<ActorDto>>> GetActors() =>
        Ok(await _context.Actors
            .Select(a => new ActorDto { Id = a.Id, Name = a.Name, BirthYear = a.BirthYear })
            .ToListAsync());

    // GET /api/actors/{id}
    [HttpGet("actors/{id:int}")]
    public async Task<ActionResult<ActorDto>> GetActor(int id)
    {
        var actor = await _context.Actors.Where(a => a.Id == id)
            .Select(a => new ActorDto { Id = a.Id, Name = a.Name, BirthYear = a.BirthYear })
            .FirstOrDefaultAsync();
        return actor is null ? NotFound() : Ok(actor);
    }

    // Post /api/actors
    [HttpPost("actors")]
    public async Task<ActionResult<ActorDto>> CreateActor(ActorDto dto)
    {
        var actor = new Actor { Name = dto.Name, BirthYear = dto.BirthYear };
        _context.Actors.Add(actor);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetActor), new { id = actor.Id }, dto);
    }

    // PUT /api/actors/{id}
    [HttpPut("actors/{id:int}")]
    public async Task<ActionResult> UpdateActor(int id, ActorDto dto)
    {
        var actor = await _context.Actors.FindAsync(id);
        if (actor is null) return NotFound();
        actor.Name = dto.Name;
        actor.BirthYear = dto.BirthYear;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // Post /api/movies/{movieIde}/actors/{actorId} - add actor to movie, N:M
    [HttpPost("movies/{movieId:int}/actors/{actorId:int}")]
    public async Task<IActionResult> AddActorToMovie(int movieId, int actorId)
    {
        var movie = await _context.Movies.Include(m => m.Actors).FirstOrDefaultAsync(m => m.Id == movieId);
        if (movie is null) return NotFound($"Movie with id {movieId} not found.");

        var actor = await _context.Actors.FindAsync(actorId);
        if (actor is null) return NotFound($"Actor with id {actorId} not found.");

        if (movie.Actors.Any(a => a.Id == actorId)) return 
            Conflict($"Actor with id {actorId} is already in movie with id {movieId}.");

        movie.Actors.Add(actor);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
