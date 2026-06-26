using AutoMapper;
using MovieContracts;
using MovieCore.DomainContracts;
using MovieCore.DTOs;

namespace MovieServices;

public class MovieService(IUnitOfWork uow, IMapper mapper) : IMovieService
{
    public Task<MovieDto> GetAsync(int id) => Task.FromResult(new MovieDto()); // stub
    public Task<MovieDto> CreateAsync(MovieCreateDto dto) => throw new NotImplementedException();
}