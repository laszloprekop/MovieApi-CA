using Microsoft.EntityFrameworkCore;
using MovieCore.DomainContracts;
using MovieCore.Models;

namespace MovieData.Repositories;

public class MovieRepository(MovieContext context) : IMovieRepository
{
    public async Task<IEnumerable<Movie>> GetAllAsync() => await context.Movies.ToListAsync();

    public Task<Movie?> GetAsync(int id) => context.Movies.FindAsync(id).AsTask();

    public Task<bool> AnyAsync(int id) => context.Movies.AnyAsync(m => m.Id == id);
    
    public Task<Movie?> GetWithDetailsAsync(int id) => context.Movies
        .Include(m => m.Details)
        .Include(m => m.Reviews)
        .Include(m => m.Actors)
        .FirstOrDefaultAsync(m => m.Id == id);

    public void Add(Movie movie) => context.Movies.Add(movie);
    public void Update(Movie movie) => context.Movies.Update(movie);
    public void Remove(Movie movie) => context.Movies.Remove(movie);
}