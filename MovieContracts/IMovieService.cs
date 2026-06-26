using MovieCore.DTOs;

namespace MovieContracts;

public interface IMovieService
{
    Task<MovieDto> GetAsync(int id);
    Task<MovieDto> CreateAsync(MovieCreateDto dto);
}