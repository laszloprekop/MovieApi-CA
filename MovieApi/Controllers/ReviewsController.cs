using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MovieCore.DomainContracts;
using MovieCore.DTOs;
using MovieCore.Models;
using MovieData;

namespace MovieApi.Controllers;

[ApiController]
[Route("api")]
public class ReviewsController(IUnitOfWork iuw, MovieContext context) : ControllerBase
{
    // GET: api/movies/{movieId}/reviews
    [HttpGet("movies/{movieId}/reviews")]
    public async Task<ActionResult<IEnumerable<ReviewDto>>> GetReviews(int movieId)
    {
        if (!await iuw.Movies.AnyAsync(movieId)) return NotFound();

        var reviews = await iuw.Reviews.GetByMovieIdAsync(movieId);
        return Ok(reviews.Select(r => new ReviewDto
        {
            Id = r.Id,
            ReviewerName = r.ReviewerName,
            Comment = r.Comment,
            Rating = r.Rating
        }).ToList());
    }

    // POST: api/movies/{movieId}/reviews
    [HttpPost("movies/{movieId:int}/reviews")]
    public async Task<ActionResult<ReviewDto>> CreateReview(int movieId, ReviewDto dto)
    {
        if (!await iuw.Movies.AnyAsync(movieId)) return NotFound();

        var review = new Review
        {
            MovieId = movieId,
            ReviewerName = dto.ReviewerName,
            Comment = dto.Comment,
            Rating = dto.Rating
        };
        iuw.Reviews.Add(review);
        await iuw.CompleteAsync();
        dto.Id = review.Id;
        return CreatedAtAction(nameof(GetReviews), new { movieId }, dto);
    }

    // DELETE /api/reviews/{id}
    [HttpDelete("reviews/{id:int}")]
    public async Task<ActionResult> DeleteReview(int id)
    {
        var review = await iuw.Reviews.GetAsync(id);
        if (review is null) return NotFound();

        iuw.Reviews.Remove(review);
        await iuw.CompleteAsync();
        return NoContent();
    }
}
