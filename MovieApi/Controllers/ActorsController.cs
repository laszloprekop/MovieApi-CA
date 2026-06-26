using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MovieCore.DomainContracts;
using MovieCore.DTOs;
using MovieCore.Models;
using MovieData;

namespace MovieApi.Controllers;

[ApiController]
[Route("api")]
public class ActorsController(IUnitOfWork iuw) : ControllerBase
{
    // GET: api/actors
    [HttpGet("actors")]
    public async Task<ActionResult<IEnumerable<ActorDto>>> GetActors()
    {
        var actors = await iuw.Actors.GetAllAsync();
        return Ok(actors
            .Select(a => new ActorDto { Id = a.Id, Name = a.Name, BirthYear = a.BirthYear }).ToList());
    }

    // GET /api/actors/{id}
    [HttpGet("actors/{id:int}")]
    public async Task<ActionResult<ActorDto>> GetActor(int id)
    {
        var actor = await iuw.Actors.GetAsync(id);
        return actor is null
            ? NotFound()
            : Ok(new ActorDto { Id = actor.Id, Name = actor.Name, BirthYear = actor.BirthYear });
    }

    // Post /api/actors
    [HttpPost("actors")]
    public async Task<ActionResult<ActorDto>> CreateActor(ActorDto dto)
    {
        var actor = new Actor { Name = dto.Name, BirthYear = dto.BirthYear };
        iuw.Actors.Add(actor);
        await iuw.CompleteAsync();
        dto.Id = actor.Id;
        return CreatedAtAction(nameof(GetActor), new { id = actor.Id }, dto);
    }

    // PUT /api/actors/{id}
    [HttpPut("actors/{id:int}")]
    public async Task<ActionResult> UpdateActor(int id, ActorDto dto)
    {
        var actor = await iuw.Actors.GetAsync(id);
        if (actor is null) return NotFound();
        actor.Name = dto.Name;
        actor.BirthYear = dto.BirthYear;
        await iuw.CompleteAsync();
        return NoContent();
    }

    // Post /api/movies/{movieIde}/actors/{actorId} - add actor to movie, N:M
    [HttpPost("movies/{movieId:int}/actors/{actorId:int}")]
    public async Task<IActionResult> AddActorToMovie(int movieId, int actorId)
    {
        var movie = await iuw.Movies.GetWithActorAsync(movieId);
        if (movie is null) return NotFound($"Movie with id {movieId} not found.");

        var actor = await iuw.Actors.GetAsync(actorId);
        if (actor is null) return NotFound($"Actor with id {actorId} not found.");

        if (movie.Actors.Any(a => a.Id == actorId))
            return
                Conflict($"Actor with id {actorId} is already in movie with id {movieId}.");

        movie.Actors.Add(actor);
        await iuw.CompleteAsync();
        return NoContent();
    }
}