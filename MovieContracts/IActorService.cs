using MovieCore.DTOs;

namespace MovieContracts;

public interface IActorService
{
    Task<IEnumerable<ActorDto>> GetAllAsync();
    Task<ActorDto> GetAsync(int id);
    Task<ActorDto> CreateAsync(ActorDto dto);
    Task UpdateAsync(int id, ActorDto dto);
    Task AddToMovieAsync(int movieId, int actorId);
}