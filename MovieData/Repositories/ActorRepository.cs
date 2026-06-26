using Microsoft.EntityFrameworkCore;
using MovieCore.DomainContracts;
using MovieCore.Models;

namespace MovieData.Repositories;

public class ActorRepository(MovieContext context): IActorRepository
{
    public async Task<IEnumerable<Actor>> GetAllAsync() =>
        await context.Actors.ToListAsync();

    public Task<Actor?> GetAsync(int id) => context.Actors.FindAsync(id).AsTask();

    public Task<bool> AnyAsync(int id) => context.Actors.AnyAsync(a => a.Id == id);
 
    public void Add(Actor actor) => context.Actors.Add(actor);
    public void Update(Actor actor) => context.Actors.Update(actor);
    public void Remove(Actor actor) => context.Actors.Remove(actor);
}