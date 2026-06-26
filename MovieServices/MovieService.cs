using AutoMapper;
using MovieContracts;
using MovieCore.DomainContracts;
using MovieCore.DTOs;
using MovieCore.Exceptions;
using MovieCore.Models;

namespace MovieServices;

public class MovieService(IUnitOfWork uow, IMapper mapper) : IMovieService
{
    public async Task<MovieDto> GetAsync(int id)
    {
        var movie = await uow.Movies.GetAsync(id);
        if (movie == null)
        {
            throw new NotFoundException($"Movie {id} not found");
        }

        return mapper.Map<MovieDto>(movie);
    }

    public async Task<MovieDto> CreateAsync(MovieCreateDto dto)
    {
        var movie = mapper.Map<Movie>(dto);
        uow.Movies.Add(movie);
        await uow.CompleteAsync();
        return mapper.Map<MovieDto>(movie);
    }
}