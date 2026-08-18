using MovieCore.Models;

namespace MovieCore.DomainContracts;

public interface IGenreRepository
{
    Task<List<Genre>> GetAllAsync();
    Task<List<Genre>> GetByIdsAsync(IEnumerable<int> ids);
}