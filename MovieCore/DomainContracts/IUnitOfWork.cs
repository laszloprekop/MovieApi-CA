namespace MovieCore.DomainContracts;

public interface IUnitOfWork
{
    IMovieRepository Movies { get; }
    Task CompleteAsync();
}