using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MovieContracts;
using MovieCore;
using MovieCore.DTOs;

namespace MoviePresentation.Controllers;

[ApiController]
[Route("api")]
public class ActorsController(IServiceManager services) : ControllerBase
{
    // GET: api/actors
    [HttpGet("actors")]
    public async Task<ActionResult<IEnumerable<ActorDto>>> GetActors()
    {
        var result = await services.ActorService.GetAllAsync();
        if (!result.IsSuccess) return ToProblem(result);
        return Ok(result.Value);
    }

    // GET /api/actors/{id}
    [HttpGet("actors/{id:int}")]
    public async Task<ActionResult<ActorDto>> GetActor(int id)
    {
        var result = await services.ActorService.GetAsync(id);
        if (!result.IsSuccess) return ToProblem(result);
        return Ok(result.Value);
    }

    // Post /api/actors
    [HttpPost("actors")]
    public async Task<ActionResult<ActorDto>> CreateActor(ActorDto dto)
    {
        var result = await services.ActorService.CreateAsync(dto);
        if (!result.IsSuccess) return ToProblem(result);
        return CreatedAtAction(nameof(GetActor), new { id = result.Value.Id }, result.Value);
    }

    // PUT /api/actors/{id}
    [HttpPut("actors/{id:int}")]
    public async Task<IActionResult> UpdateActor(int id, ActorDto dto)
    {
        var result = await services.ActorService.UpdateAsync(id, dto);
        if (!result.IsSuccess) return ToProblem(result);
        return NoContent();
    }

    // Post /api/movies/{movieIde}/actors/{actorId} - add actor to movie, N:M
    [HttpPost("movies/{movieId:int}/actors/{actorId:int}")]
    public async Task<IActionResult> AddActorToMovie(int movieId, int actorId)
    {
        var result = await services.ActorService.AddToMovieAsync(movieId, actorId);
        if (!result.IsSuccess) return ToProblem(result);
        return NoContent();
    }

    private ObjectResult ToProblem(Result result) =>
        Problem(detail: result.Error, statusCode: result.ErrorType switch
        {
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.BusinessRule => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        });
}