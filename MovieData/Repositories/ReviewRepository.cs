using Microsoft.EntityFrameworkCore;
using MovieCore.DomainContracts;
using MovieCore.Models;

namespace MovieData.Repositories;

public class ReviewRepository(MovieContext context) : IReviewRepository
{
    public async Task<IEnumerable<Review>> GetByMovieIdAsync(int movieId) =>
        await context.Reviews.Where(r => r.MovieId == movieId).ToListAsync();
    
    public Task<Review?> GetAsync(int id) => context.Reviews.FindAsync(id).AsTask();
    public void Add(Review review) => context.Reviews.Add(review);
    public void Remove(Review review) => context.Reviews.Remove(review);
}