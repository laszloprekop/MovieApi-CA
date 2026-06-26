using MovieCore.DTOs;

namespace MovieContracts;

public interface IMovieService
{
    Task<IEnumerable<MovieDto>> GetAllAsync(string? genre, int? year, string? actor);
    Task<MovieDto> GetAsync(int id);
    Task<MovieDetailDto> GetDetailsAsync(int id);
    Task<MovieDto> CreateAsync(MovieCreateDto dto);
    Task UpdateAsync(int id, MovieUpdateDto dto);
    Task DeleteAsync(int id);
}