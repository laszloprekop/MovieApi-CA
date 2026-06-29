namespace MovieCore.DomainContracts;

using MovieCore.Models;

public interface IMovieRepository
{
    Task<IEnumerable<Movie>> GetAllAsync();
    Task<Movie?> GetAsync(int id);
    Task<bool> AnyAsync(int id);
    Task<Movie?> GetWithDetailsAsync(int id);
    Task<Movie?> GetWithActorsAsync(int MovieId);
    void Add(Movie movie);
    void Update(Movie movie);
    void Remove(Movie movie);
    Task<bool> TitleExistsAsync(string title, int? excludeId = null);
    Task<Movie?> GetWithReviewsAsync(int id);
    Task<IEnumerable<Movie>> GetAllWithReviewsAsync();
}