namespace MovieCore.DomainContracts;

using MovieCore.Models;

public interface IMovieRepository
{
    Task<IEnumerable<Movie>> GetAllAsync();
    Task<Movie?> GetAsync(int id);
    Task<bool> AnyAsync(int id);
    Task<Movie?> GetWithDetailsAsync(int id);
    void Add(Movie movie);
    void Update(Movie movie);
    void Remove(Movie movie);
}