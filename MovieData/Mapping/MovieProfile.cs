using AutoMapper;
using MovieCore.DTOs;
using MovieCore.Models;

namespace MovieData.Mapping;

public class MovieProfile : Profile
{
    public MovieProfile()
    {
        CreateMap<Movie, MovieDto>()
            .ForMember(
                d => d.Genre,
                o => o.MapFrom(s => string.Join(", ", s.Genres.Select(g => g.Name))));

        // Movie's Id is DB-generated and its navigations are populated by EF / later
        // business logic — not from the create DTO. Ignore them so the config is
        // validation-clean (AssertConfigurationIsValid) and the intent is explicit.
        CreateMap<MovieCreateDto, Movie>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.Details, o => o.Ignore())
            .ForMember(d => d.Reviews, o => o.Ignore())
            .ForMember(d => d.Actors, o => o.Ignore())
            .ForMember(d => d.Genres, o => o.Ignore());
    }
}