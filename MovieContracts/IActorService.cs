using MovieCore;
using MovieCore.DTOs;

namespace MovieContracts;

public interface IActorService
{
    Task<Result<IEnumerable<ActorDto>>> GetAllAsync();
    Task<Result<ActorDto>> GetAsync(int id);
    Task<Result<ActorDto>> CreateAsync(ActorDto dto);
    Task<Result> UpdateAsync(int id, ActorDto dto);
    Task<Result> AddToMovieAsync(int movieId, int actorId);
}