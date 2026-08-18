using MovieCore.DTOs;

namespace MovieContracts;

public interface IGenreService
{
    Task<IEnumerable<GenreDto>> GetAllAsync();
}
