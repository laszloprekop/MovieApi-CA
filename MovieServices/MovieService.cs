using AutoMapper;
using MovieContracts;
using MovieCore.DomainContracts;
using MovieCore.DTOs;
using MovieCore.Exceptions;
using MovieCore.Models;

namespace MovieServices;

public class MovieService(IUnitOfWork uow, IMapper mapper) : IMovieService
{
    public async Task<IEnumerable<MovieDto>> GetAllAsync(string? genre, int? year, string? actor)
    {
        var movies = await uow.Movies.GetAllAsync();
        if (!string.IsNullOrWhiteSpace(genre))
            movies = movies.Where(m =>
                m.Genres.Any(g => string.Equals(g.Name, genre, StringComparison.OrdinalIgnoreCase)));
        if (year is not null) movies = movies.Where(m => m.Year == year);
        if (!string.IsNullOrWhiteSpace(actor)) movies = movies.Where(m => m.Actors.Any(a => a.Name == actor));
        return mapper.Map<IEnumerable<MovieDto>>(movies);
    }

    public async Task<MovieDto> GetAsync(int id)
    {
        var movie = await uow.Movies.GetAsync(id)
                    ?? throw new NotFoundException($"Movie {id} not found");
        return mapper.Map<MovieDto>(movie);
    }

    public async Task<MovieDetailDto> GetDetailsAsync(int id)
    {
        var movie = await uow.Movies.GetWithDetailsAsync(id)
                    ?? throw new NotFoundException($"Movie {id} not found");

        return new MovieDetailDto
        {
            Id = movie.Id,
            Title = movie.Title,
            Year = movie.Year,
            Genre = string.Join(", ", movie.Genres.Select(g => g.Name)),
            Duration = movie.Duration,
            Synopsis = movie.Details?.Synopsis,
            Language = movie.Details?.Language,
            Budget = movie.Details?.Budget ?? 0,
            Reviews = movie.Reviews.Select(r => new ReviewDto
            {
                Id = r.Id, ReviewerName = r.ReviewerName, Comment = r.Comment, Rating = r.Rating
            }).ToList(),
            Actors = movie.Actors.Select(a => new ActorDto
            {
                Id = a.Id, Name = a.Name, BirthYear = a.BirthYear
            }).ToList()
        };
    }

    public async Task<MovieDto> CreateAsync(MovieCreateDto dto)
    {
        if (dto.GenreIds is null || dto.GenreIds.Count == 0)
            throw new BusinessRuleException("A movie must have at least one genre.");

        var genres = await uow.Genres.GetByIdsAsync(dto.GenreIds);
        if (genres.Count != dto.GenreIds.Distinct().Count())
            throw new BusinessRuleException("One or more genres do not exist.");

        var movie = mapper.Map<Movie>(dto);
        movie.Genres = genres;
        uow.Movies.Add(movie);
        await uow.CompleteAsync();
        return mapper.Map<MovieDto>(movie);
    }

    public async Task UpdateAsync(int id, MovieUpdateDto dto)
    {
        var movie = await uow.Movies.GetAsync(id)
                    ?? throw new NotFoundException($"Movie {id} not found");
        movie.Title = dto.Title;
        movie.Year = dto.Year;
        movie.Duration = dto.Duration;
        await uow.CompleteAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var movie = await uow.Movies.GetAsync(id)
                    ?? throw new NotFoundException($"Movie {id} not found");
        uow.Movies.Remove(movie);
        await uow.CompleteAsync();
    }
}