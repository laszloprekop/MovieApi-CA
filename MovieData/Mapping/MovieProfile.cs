using AutoMapper;
using MovieCore.DTOs;
using MovieCore.Models;

namespace MovieData.Mapping;

public class MovieProfile : Profile
{
    public MovieProfile()
    {
        CreateMap<Movie, MovieDto>();
        CreateMap<MovieCreateDto, Movie>();
    }
}