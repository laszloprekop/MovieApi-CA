using MovieCore.DTOs;

namespace MovieContracts;

public interface IReviewService
{
    Task<IEnumerable<ReviewDto>> GetByMovieIdAsync(int movieId);
    Task<ReviewDto> CreateAsync(int movieId, ReviewDto dto);
    Task DeleteAsync(int id);
}