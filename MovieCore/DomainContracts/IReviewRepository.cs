using MovieCore.Models;

namespace MovieCore.DomainContracts;

public interface IReviewRepository
{
    Task<IEnumerable<Review>> GetByMovieIdAsync(int movieId);
    Task<Review?> GetAsync(int id);
    void Add(Review review);
    void Remove(Review review);
}