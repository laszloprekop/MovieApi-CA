using AutoMapper;
using MovieContracts;
using MovieCore.DomainContracts;
using MovieCore.DTOs;
using MovieCore.Exceptions;
using MovieCore.Models;

namespace MovieServices;

public class MovieService(IUnitOfWork uow, IMapper mapper) : IMovieService
{
    private static IEnumerable<Movie> ApplyFilters(IEnumerable<Movie> movies, string? genre, int? year, string? actor)
    {
        if (!string.IsNullOrWhiteSpace(genre))
            movies = movies.Where(m =>
                m.Genres.Any(g => string.Equals(g.Name, genre, StringComparison.OrdinalIgnoreCase)));
        if (year is not null) movies = movies.Where(m => m.Year == year);
        if (!string.IsNullOrWhiteSpace(actor))
            movies = movies.Where(m =>
                m.Actors.Any(a => string.Equals(a.Name, actor, StringComparison.OrdinalIgnoreCase)));
        return movies;
    }

    public async Task<IEnumerable<MovieDto>> GetAllAsync(string? genre, int? year, string? actor) =>
        mapper.Map<IEnumerable<MovieDto>>(ApplyFilters(await uow.Movies.GetAllAsync(), genre, year, actor));

    public async Task<PagedResult<MovieDto>> GetPageAsync(string? genre, int? year, string? actor,
        PaginationParameters paging)
    {
        var movies = ApplyFilters(await uow.Movies.GetAllAsync(), genre, year, actor);

        var enumerable = movies as Movie[] ?? movies.ToArray();
        var total = enumerable.Count();
        var items = enumerable.Skip((paging.Page - 1) * (paging.PageSize)).Take(paging.PageSize);
        return new PagedResult<MovieDto>(
            mapper.Map<List<MovieDto>>(items),
            new PaginationMeta(paging.Page, paging.PageSize, total)
        );
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
        if (await uow.Movies.TitleExistsAsync(dto.Title))
            throw new BusinessRuleException($"Movie with title {dto.Title} already exists.");

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
        if (await uow.Movies.TitleExistsAsync(dto.Title, id))
            throw new BusinessRuleException($"Movie with title {dto.Title} already exists.");
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

    public async Task<MoviePatchDto> GetPatchModelAsync(int id)
    {
        var movie = await uow.Movies.GetWithDetailsAsync(id)
                    ?? throw new NotFoundException($"Movie {id} not found");
        return new MoviePatchDto
        {
            Title = movie.Title,
            Year = movie.Year,
            Duration = movie.Duration,
            Synopsis = movie.Details?.Synopsis,
            Language = movie.Details?.Language,
            Budget = movie.Details?.Budget ?? 0
        };
    }

    public async Task ApplyPatchAsync(int id, MoviePatchDto dto)
    {
        var movie = await uow.Movies.GetWithDetailsAsync(id)
                    ?? throw new NotFoundException($"Movie {id} not found");

        if (await uow.Movies.TitleExistsAsync(dto.Title, id))
            throw new BusinessRuleException($"Movie with title {dto.Title} already exists.");
        if (dto.Budget < 0)
            throw new BusinessRuleException("Budget cannot be negative.");
        if (MovieRules.IsDocumentary(movie) && dto.Budget > 1_000_000m)
            throw new BusinessRuleException("Documentaries cannot have a budget greater than $1,000,000.");

        movie.Title = dto.Title;
        movie.Year = dto.Year;
        movie.Duration = dto.Duration;

        if (movie.Details is null)
        {
            movie.Details = new MovieDetails
            {
                Synopsis = dto.Synopsis ?? string.Empty,
                Language = dto.Language ?? string.Empty,
                Budget = dto.Budget
            };
        }
        else
        {
            movie.Details.Synopsis = dto.Synopsis ?? string.Empty;
            movie.Details.Language = dto.Language ?? string.Empty;
            movie.Details.Budget = dto.Budget;
        }

        await uow.CompleteAsync();
    }
}