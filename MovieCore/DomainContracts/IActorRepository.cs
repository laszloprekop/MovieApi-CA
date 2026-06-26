using MovieCore.Models;

namespace MovieCore.DomainContracts;

public interface IActorRepository
{
    Task<IEnumerable<Actor>> GetAllAsync();
    Task<Actor?> GetAsync(int id);
    Task<bool> AnyAsync(int id);
    void Add(Actor actor);
    void Update(Actor actor);
    void Remove(Actor actor);
}