namespace MovieData.Repositories;

using MovieCore.DomainContracts;

public class UnitOfWork(MovieContext context): IUnitOfWork
{
    public IMovieRepository Movies { get; } = new MovieRepository(context);
    public IReviewRepository Reviews { get; } = new ReviewRepository(context);
    public IActorRepository Actors { get; } = new ActorRepository(context);
    public Task CompleteAsync() => context.SaveChangesAsync();
    public IGenreRepository Genres { get; } = new GenreRepository(context);
}