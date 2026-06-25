using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MovieCore.DTOs;
using MovieCore.Models;

namespace MovieApi.Controllers;

[ApiController]
[Route("api")]
public class ReviewsController : ControllerBase
{
    private readonly MovieContext _context;
    public ReviewsController(MovieContext context) => _context = context;

    // GET: api/movies/{movieId}/reviews
    [HttpGet("movies/{movieId}/reviews")]
    public async Task<ActionResult<IEnumerable<ReviewDto>>> GetReviews(int movieId)
    {
        if (!await _context.Movies.AnyAsync(m => m.Id == movieId)) return NotFound();

        var reviews = await _context.Reviews
            .Where(r => r.MovieId == movieId)
            .Select(r => new ReviewDto
            {
                Id = r.Id,
                ReviewerName = r.ReviewerName,
                Comment = r.Comment,
                Rating = r.Rating
            }).ToListAsync();
        return Ok(reviews);
    }

    // POST: api/movies/{movieId}/reviews
    [HttpPost("movies/{movieId:int}/reviews")]
    public async Task<ActionResult<ReviewDto>> CreateReview(int movieId, ReviewDto dto)
    {
        if (!await _context.Movies.AnyAsync(m => m.Id == movieId)) return NotFound();

        var review = new Review
        {
            MovieId = movieId,
            ReviewerName = dto.ReviewerName,
            Comment = dto.Comment,
            Rating = dto.Rating
        };
        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();
        dto.Id = review.Id;
        return CreatedAtAction(nameof(GetReviews), new { movieId }, dto);
    }

    // DELETE /api/reviews/{id}
    [HttpDelete("reviews/{id:int}")]
    public async Task<ActionResult> DeleteReview(int id)
    {
        var review = await _context.Reviews.FindAsync(id);
        if (review is null) return NotFound();
        
        _context.Reviews.Remove(review);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
