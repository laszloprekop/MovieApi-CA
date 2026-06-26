namespace MovieContracts;

public interface IServiceManager
{
    IMovieService MovieService { get; }
    IReviewService ReviewService { get; }
    IActorService ActorService { get; }
}