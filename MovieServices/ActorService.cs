using MovieContracts;
using MovieCore.DomainContracts;
using MovieCore.DTOs;
using MovieCore.Exceptions;
using MovieCore.Models;

namespace MovieServices;

public class ActorService(IUnitOfWork uow) : IActorService
{
    public async Task<IEnumerable<ActorDto>> GetAllAsync()
    {
        var actors = await uow.Actors.GetAllAsync();
        return actors.Select(a => new ActorDto
        {
            Id = a.Id,
            Name = a.Name,
            BirthYear = a.BirthYear
        }).ToList();
    }

    public async Task<ActorDto> GetAsync(int id)
    {
        var actor = await uow.Actors.GetAsync(id)
                    ?? throw new NotFoundException($"Actor {id} not found");
        return new ActorDto
        {
            Id = actor.Id,
            Name = actor.Name,
            BirthYear = actor.BirthYear
        };
    }

    public async Task<ActorDto> CreateAsync(ActorDto dto)
    {
        var actor = new Actor
        {
            Name = dto.Name,
            BirthYear = dto.BirthYear
        };
        uow.Actors.Add(actor);
        await uow.CompleteAsync();
        dto.Id = actor.Id;
        return dto;
    }

    public async Task UpdateAsync(int id, ActorDto dto)
    {
        var actor =  await uow.Actors.GetAsync(id)
                    ?? throw new NotFoundException($"Actor {id} not found");
        actor.Name = dto.Name;
        actor.BirthYear = dto.BirthYear;
        await uow.CompleteAsync();
    }

    public async Task AddToMovieAsync(int movieId, int actorId)
    {
        var movie = await uow.Movies.GetWithActorsAsync(movieId)
                    ?? throw new NotFoundException($"Movie {movieId} not found");
        var actor = await uow.Actors.GetAsync(actorId)
                    ?? throw new NotFoundException($"Actor {actorId} not found");

        if (movie.Actors.Any(a => a.Id == actorId))
            throw new BusinessRuleException($"Actor {actorId} is already in movie {movieId}.");
        
        movie.Actors.Add(actor);
        await uow.CompleteAsync();
    }
}