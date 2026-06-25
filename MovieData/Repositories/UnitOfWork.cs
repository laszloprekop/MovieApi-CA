namespace MovieData.Repositories;

using MovieCore.DomainContracts;

public class UnitOfWork(MovieContext context): IUnitOfWork
{
    public IMovieRepository Movies { get; } = new MovieRepository(context);
    public Task CompleteAsync() => context.SaveChangesAsync();
}