using Microsoft.AspNetCore.Mvc;
using MovieContracts;
using MovieCore.DomainContracts;
using MovieCore.DTOs;
using MovieCore.Models;

namespace MoviePresentation.Controllers;

[ApiController]
[Route("api")]
public class ActorsController(IServiceManager services) : ControllerBase
{
    // GET: api/actors
    [HttpGet("actors")]
    public async Task<ActionResult<IEnumerable<ActorDto>>> GetActors() =>
        Ok(await services.ActorService.GetAllAsync());

    // GET /api/actors/{id}
    [HttpGet("actors/{id:int}")]
    public async Task<ActionResult<ActorDto>> GetActor(int id) =>
        Ok(await services.ActorService.GetAsync(id));

    // Post /api/actors
    [HttpPost("actors")]
    public async Task<ActionResult<ActorDto>> CreateActor(ActorDto dto)
    {
        var created = await services.ActorService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetActor), new { id = created.Id }, created);
    }

    // PUT /api/actors/{id}
    [HttpPut("actors/{id:int}")]
    public async Task<ActionResult> UpdateActor(int id, ActorDto dto)
    {
        await services.ActorService.UpdateAsync(id, dto);
        return NoContent();
    }

    // Post /api/movies/{movieIde}/actors/{actorId} - add actor to movie, N:M
    [HttpPost("movies/{movieId:int}/actors/{actorId:int}")]
    public async Task<IActionResult> AddActorToMovie(int movieId, int actorId)
    {
        await services.ActorService.AddToMovieAsync(movieId, actorId);
        return NoContent();
    }
}