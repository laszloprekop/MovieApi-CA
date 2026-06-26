using Microsoft.AspNetCore.Mvc;
using MovieContracts;
using MovieCore.DomainContracts;
using MovieCore.DTOs;
using MovieCore.Models;

namespace MoviePresentation.Controllers;

[ApiController]
[Route("api")]
public class ReviewsController(IServiceManager services) : ControllerBase
{
    // GET: api/movies/{movieId}/reviews
    [HttpGet("movies/{movieId}/reviews")]
    public async Task<ActionResult<IEnumerable<ReviewDto>>> GetReviews(int movieId) =>
        Ok(await services.ReviewService.GetByMovieIdAsync(movieId));

    // POST: api/movies/{movieId}/reviews
    [HttpPost("movies/{movieId:int}/reviews")]
    public async Task<ActionResult<ReviewDto>> CreateReview(int movieId, ReviewDto dto)
    {
        var created = await services.ReviewService.CreateAsync(movieId, dto);
        return CreatedAtAction(nameof(GetReviews), new { movieId }, created);
    }

    // DELETE /api/reviews/{id}
    [HttpDelete("reviews/{id:int}")]
    public async Task<ActionResult> DeleteReview(int id)
    {
        await services.ReviewService.DeleteAsync(id);
        return NoContent();
    }
}