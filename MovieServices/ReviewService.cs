using MovieContracts;
using MovieCore.DomainContracts;
using MovieCore.DTOs;
using MovieCore.Exceptions;
using MovieCore.Models;

namespace MovieServices;

public class ReviewService(IUnitOfWork uow) : IReviewService
{
    public async Task<IEnumerable<ReviewDto>> GetByMovieIdAsync(int movieId)
    {
        if (!await uow.Movies.AnyAsync(movieId))
            throw new NotFoundException($"Movie {movieId} not found");

        var reviews = await uow.Reviews.GetByMovieIdAsync(movieId);
        return reviews.Select(r => new ReviewDto
        {
            Id = r.Id,
            ReviewerName = r.ReviewerName,
            Comment = r.Comment,
            Rating = r.Rating,
            CreatedAt = r.CreatedAt,
        }).ToList();
    }

    public async Task<ReviewDto> CreateAsync(int movieId, ReviewDto dto)
    {
        var movie = await uow.Movies.GetWithReviewsAsync(movieId)
                    ?? throw new NotFoundException($"Movie {movieId} not found");

        if (movie.Reviews.Count >= 10)
            throw new BusinessRuleException("A movie can only have 10 reviews.");

        if (DateTime.UtcNow.Year - movie.Year > 20 && movie.Reviews.Count >= 5)
            throw new BusinessRuleException("A movie can only have 5 reviews if it is older than 20 years.");

        var review = new Review
        {
            MovieId = movieId,
            ReviewerName = dto.ReviewerName,
            Comment = dto.Comment,
            Rating = dto.Rating
        };
        uow.Reviews.Add(review);
        await uow.CompleteAsync();
        dto.Id = review.Id;
        return dto;
    }

    public async Task DeleteAsync(int id)
    {
        var review = await uow.Reviews.GetAsync(id)
                     ?? throw new NotFoundException($"Review {id} not found");
        uow.Reviews.Remove(review);
        await uow.CompleteAsync();
    }
}