using MovieContracts;
using MovieCore;
using MovieCore.DomainContracts;
using MovieCore.DTOs;
using MovieCore.Models;

namespace MovieServices;

public class ActorService(IUnitOfWork uow) : IActorService
{
    public async Task<Result<IEnumerable<ActorDto>>> GetAllAsync()
    {
        var actors = await uow.Actors.GetAllAsync();
        var dtos = actors.Select(a => new ActorDto
        {
            Id = a.Id,
            Name = a.Name,
            BirthYear = a.BirthYear
        }).ToList();
        return Result.Success<IEnumerable<ActorDto>>(dtos);
    }

    public async Task<Result<ActorDto>> GetAsync(int id)
    {
        var actor = await uow.Actors.GetAsync(id);
        if (actor is null) return Result.NotFound<ActorDto>($"Actor {id} not found");
        return Result.Success(
            new ActorDto { Id = actor.Id, Name = actor.Name, BirthYear = actor.BirthYear });
    }

    public async Task<Result<ActorDto>> CreateAsync(ActorDto dto)
    {
        var actor = new Actor
        {
            Name = dto.Name,
            BirthYear = dto.BirthYear
        };
        uow.Actors.Add(actor);
        await uow.CompleteAsync();
        dto.Id = actor.Id;
        return Result.Success(dto);
    }

    public async Task<Result> UpdateAsync(int id, ActorDto dto)
    {
        var actor = await uow.Actors.GetAsync(id);
        if (actor is null) return Result.NotFound($"Actor {id} not found");
        actor.Name = dto.Name;
        actor.BirthYear = dto.BirthYear;
        await uow.CompleteAsync();
        return Result.Success();
    }

    public async Task<Result> AddToMovieAsync(int movieId, int actorId)
    {
        var movie = await uow.Movies.GetWithActorsAsync(movieId);
        if (movie is null) return Result.NotFound($"Movie {movieId} not found");
        var actor = await uow.Actors.GetAsync(actorId);
        if (actor is null) return Result.NotFound($"Actor {actorId} not found");

        if (movie.Actors.Any(a => a.Id == actorId))
            return Result.BusinessRule($"Actor {actorId} is already in movie {movieId}.");

        if (MovieRules.IsDocumentary(movie) && movie.Actors.Count >= 10)
            return Result.BusinessRule("A documentary can only have 10 actors.");

        movie.Actors.Add(actor);
        await uow.CompleteAsync();
        return Result.Success();
    }
}