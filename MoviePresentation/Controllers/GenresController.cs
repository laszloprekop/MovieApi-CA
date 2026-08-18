using Microsoft.AspNetCore.Mvc;
using MovieContracts;
using MovieCore.DTOs;

namespace MoviePresentation.Controllers;

[ApiController]
[Route("api")]
public class GenresController(IServiceManager services) : ControllerBase
{
    // GET: api/genres
    [HttpGet("genres")]
    public async Task<ActionResult<IEnumerable<GenreDto>>> GetGenres() =>
        Ok(await services.GenreService.GetAllAsync());
}
