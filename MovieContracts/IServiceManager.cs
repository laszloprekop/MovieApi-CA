namespace MovieContracts;

public interface IServiceManager
{
    IMovieService Movies { get; }
}