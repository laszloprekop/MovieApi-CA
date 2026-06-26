namespace MovieContracts;

public interface IServiceManager
{
    IMovieService MovieService { get; }
}