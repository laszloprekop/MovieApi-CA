using Microsoft.EntityFrameworkCore;
using MovieCore.DomainContracts;
using MovieCore.Models;

namespace MovieData.Repositories;

public class MovieRepository(MovieContext context) : IMovieRepository
{
    public async Task<IEnumerable<Movie>> GetAllAsync() =>
        await context.Movies.Include(m => m.Genres).Include(m => m.Actors).ToListAsync();

    public Task<Movie?> GetAsync(int id) =>
        context.Movies.Include(m => m.Genres).FirstOrDefaultAsync(m => m.Id == id);

    public Task<bool> AnyAsync(int id) => context.Movies.AnyAsync(m => m.Id == id);

    public Task<Movie?> GetWithDetailsAsync(int id) => context.Movies
        .Include(m => m.Details)
        .Include(m => m.Reviews)
        .Include(m => m.Actors)
        .Include(m => m.Genres)
        .FirstOrDefaultAsync(m => m.Id == id);

    public Task<Movie?> GetWithActorsAsync(int MovieId) => context.Movies
        .Include(m => m.Actors)
        .Include(m => m.Genres)
        .FirstOrDefaultAsync(m => m.Id == MovieId);

    public void Add(Movie movie) => context.Movies.Add(movie);
    public void Update(Movie movie) => context.Movies.Update(movie);
    public void Remove(Movie movie) => context.Movies.Remove(movie);

    public Task<bool> TitleExistsAsync(string title, int? excludeId = null) =>
        context.Movies.AnyAsync(m => m.Title == title && m.Id != excludeId);

    public Task<Movie?> GetWithReviewsAsync(int id) =>
        context.Movies.Include(m => m.Reviews).FirstOrDefaultAsync(m => m.Id == id);

    public async Task<IEnumerable<Movie>> GetAllWithReviewsAsync() =>
        await context.Movies.Include(m => m.Reviews).ToListAsync();
}